﻿using System;
using System.Linq;
using TestSortingProblem.Handlers;
using TestSortingProblem.Structures;

namespace TestSortingProblem.GeneticAlgorithm
{
    public class Genome
    {
        public int Size { get; }
        private readonly Instance _instance;
        private int[] _startingTimes;
        private int[] _endingTimes;
        private string[] _machines;
        private Scheduler[] _machineSchedulers;
        private Scheduler[] _resourceSchedulers;
	    public int Fitness;

        private Schedule _instanceSchedule;
        private Schedule _dependencySchedule;

        public Genome(Instance instance)
        {
            Size = instance.Tests.Length;
            _instance = instance;
	        Fitness = int.MaxValue;
            _instanceSchedule = new Schedule();
            _dependencySchedule = new Schedule();
            InitArrays(instance);
        }

        public Genome(int[] startingTimes, int[] endingTimes, string[] machines, Instance instance)
        {
	        Size = instance.Tests.Length;
            _instance = instance;
            var startingLength = startingTimes.Length;
            var endingLength = endingTimes.Length;
            var machinesLength = machines.Length;
            if (Size != startingLength || Size != machinesLength || Size != endingLength)
                throw new ArgumentException("The arrays must be of the same size");

            Size = startingLength;
            InitArrays(instance);
            SetMachines(machines);
            SetStarts(startingTimes);
            SetEndings(endingTimes);
        }

        private void InitArrays(Instance instance)
        {
            _machines = new string[Size];
            _startingTimes = new int[Size];
            _endingTimes = new int[Size];
            InitSchedulers(instance.Machines, instance.Resources, instance.ResourcesCount);
        }

        private void InitSchedulers(string[] machines, string[] resources, int[] resourcesCountList)
        {
            _machineSchedulers = new Scheduler[machines.Length];
            _resourceSchedulers = new Scheduler[resources.Length];
            for (var i = 0; i < machines.Length; i++)
                _machineSchedulers[i] = new Scheduler(1, machines[i]);
            for (var i = 0; i < resources.Length; i++)
                _resourceSchedulers[i] = new Scheduler(resourcesCountList[i], resources[i]);
        }

        private void FindSchedule(int index)
        {
	        _instanceSchedule.StartTime = int.MaxValue;
	        Test chosenTest = _instance.Tests[index];
            int length = chosenTest.Length;
	        int machineIndex = -1;
	        int resourceIndex = -1;

			for (int i = 0; i < _machineSchedulers.Length; i++)
			{
				if (chosenTest.Machines.Count != 0 && !chosenTest.Machines.Contains(_machineSchedulers[i].Name))
					continue;

				Schedule tempSchedule;
				if (chosenTest.Resources.Count == 0)
				{
					if (!_machineSchedulers[i].CanFit(length, null, out tempSchedule, out _)) continue;
					
					if (tempSchedule.StartTime >= _instanceSchedule.StartTime) continue;
					machineIndex = i;
					_instanceSchedule.Copy(tempSchedule);
				}
				else
				{
					for (int j = 0; j < _resourceSchedulers.Length; j++)
					{
						if (!chosenTest.Resources.Contains(_resourceSchedulers[j].Name)) continue;

						if (!_machineSchedulers[i].CanFit(length, _resourceSchedulers[j], out tempSchedule, out var tempDependancySchedule)) continue;
						
						if (tempSchedule.StartTime >= _instanceSchedule.StartTime) continue;
						machineIndex = i;
						resourceIndex = j;
						_instanceSchedule.Copy(tempSchedule);
						_dependencySchedule.Copy(tempDependancySchedule);
					}
				}
            }

	        Scheduler dependency = resourceIndex != -1 ? _resourceSchedulers[resourceIndex] : null;

            _machineSchedulers[machineIndex].Add(length, index, _instanceSchedule, dependency, _dependencySchedule);
	        _machines[index] = _machineSchedulers[machineIndex].Name;
	        _startingTimes[index] = _instanceSchedule.StartTime;
	        _endingTimes[index] = _instanceSchedule.StartTime + length;
        }

        public bool SwapPlaces(int firstIndex, int secondIndex)
        {
            string firstMachine = _machines[firstIndex];
            int firstStart = _startingTimes[firstIndex];
            Test firstTest = _instance.Tests[firstIndex];
            Test secondTest = _instance.Tests[secondIndex];
            
            RemoveTestFromMachine(firstIndex);
            RemoveTestFromMachine(secondIndex);

            if(firstTest.Resources.Count != 0)
                RemoveTestFromResource(firstIndex);
            if(secondTest.Resources.Count != 0)
                RemoveTestFromResource(secondIndex);    
            FindSchedule(secondIndex);
            FindSchedule(firstIndex);

            return !_machines[firstIndex].Equals(firstMachine) || _startingTimes[firstIndex] != firstStart;
        }

        public void ScrambleGenes(int firstIndex, int secondIndex, Random rand)
        {
            for(int i = firstIndex; i <= secondIndex; i++)
            {
                Test currentTest = _instance.Tests[i];
                RemoveTestFromMachine(i);
                if(currentTest.Resources.Count != 0)
                    RemoveTestFromResource(i);
            }
            int[] randomChoice = Enumerable.Range(0, secondIndex - firstIndex + 1).OrderBy(x => rand.Next()).ToArray();
            
            for (int i = 0; i < randomChoice.Length; i++)
            {
                FindSchedule(firstIndex + randomChoice[i]);
            }
        }

        public void Randomize(Random rand)
        {
            int time = rand.Next(Fitness);
            RemoveTestsAfter(time);
            for(int i = 0; i < Size; i++)
            {
                if(_startingTimes[i] == -1)
                {
                    FindSchedule(i);
                }
            }
        }

        public int[] GetEndingTimes()
        {
            return _endingTimes;
        }

        public string[] GetMachines()
        {
            return _machines;
        }

        public int[] GetStartingTimes()
        {
            return _startingTimes;
        }

        public Scheduler GetMachineScheduler(int index)
        {
            return _machineSchedulers[index];
        }
        
        public Scheduler CopyMachineScheduler(int index)
        {
            var newMachineScheduler = new Scheduler(_machineSchedulers[index].ResourceCount, _machineSchedulers[index].Name);
            newMachineScheduler.Copy(_machineSchedulers[index]);
            return newMachineScheduler;
        }
        
        public Scheduler GetResourceScheduler(int index)
        {
            return _resourceSchedulers[index];
        }
        
        public Scheduler CopyResourceeScheduler(int index)
        {
            var newResourceScheduler = new Scheduler(_resourceSchedulers[index].ResourceCount, _resourceSchedulers[index].Name);
            newResourceScheduler.Copy(_resourceSchedulers[index]);
            return newResourceScheduler;
        }

        public void SetMachines(string[] machines)
        {
            if (machines.Length != Size)
                return;
            for (var i = 0; i < Size; i++)
            {
                _machines[i] = machines[i];
            }
        }

        public void SetStarts(int[] startingTimes)
        {
            if (startingTimes.Length != Size)
                return;
            for (var i = 0; i < Size; i++)
            {
                _startingTimes[i] = startingTimes[i];
            }
        }
        
        public void SetEndings(int[] endingTimes)
        {
            if (endingTimes.Length != Size)
                return;
            for (var i = 0; i < Size; i++)
            {
                _endingTimes[i] = endingTimes[i];
            }
        }

	    public int FirstStart()
	    {
			int minStart = int.MaxValue;

		    foreach (int start in _startingTimes)
		    {
			    if (start < minStart)
				    minStart = start;
		    }
		    return minStart;
	    }

	    public int LastEnd()
	    {
		    int maxEnd = int.MinValue;
			
		    foreach (int endingTime in _endingTimes)
		    {
			    if (endingTime > maxEnd)
				    maxEnd = endingTime;
		    }
		    return maxEnd;
	    }

        public void Copy(Genome newState)
        {
            if (Size != newState.Size)
                return;
            for (var i = 0; i < newState.Size; i++)
            {
                _startingTimes[i] = newState._startingTimes[i];
                _endingTimes[i] = newState._endingTimes[i];
                _machines[i] = newState._machines[i];
            }

            for (var i = 0; i < newState._machineSchedulers.Length; i++)
            {
                _machineSchedulers[i].Copy(newState.CopyMachineScheduler(i));
            }
	        for (var i = 0; i < newState._resourceSchedulers.Length; i++)
	        {
				_resourceSchedulers[i].Copy(newState.CopyResourceeScheduler(i));
			}
	        Fitness = newState.Fitness;
        }

	    public static string ParseTimes(Genome genome)
	    {
		    return "begins at: " + genome.FirstStart() + ", ends at: " + genome.LastEnd();
	    }

        public static Genome RandomGenome(Instance instance)
        {
            Random rand = new Random();
            Genome result = new Genome(instance);
            int[] randomChoice = Enumerable.Range(0, result.Size).OrderBy(x => rand.Next()).ToArray();
            for (int i = 0; i < result.Size; i++)
            {
                result.FindSchedule(randomChoice[i]);
            }
            return result;
        }

        private void RemoveTestFromMachine(int testIndex)
        {
            for(int i = 0; i < _instance.Machines.Length; i++)
            {
                if(_machineSchedulers[i].Remove(testIndex))
                    break;
            }
        }

        private void RemoveTestFromResource(int testIndex)
        {
            for(int i = 0; i < _instance.Resources.Length; i++)
            {
                if (!_instance.Tests[testIndex].Resources.Contains(_resourceSchedulers[i].Name)) continue;

                if(_resourceSchedulers[i].Remove(testIndex))
                    break;
            }
        }

        private void RemoveTestsAfter(int time)
        {
            for(int i = 0; i < _machineSchedulers.Length; i++)
            {
                _machineSchedulers[i].RemoveAfter(time);
            }

            for(int i = 0; i < _instance.Resources.Length; i++)
            {
                _resourceSchedulers[i].RemoveAfter(time);
            }
            UpdateArrays(time);
        }

        private void UpdateArrays(int time)
        {
            for(int i = 0; i < Size; i++)
            {
                if(_startingTimes[i] >= time)
                {
                    _startingTimes[i] = -1;
                    _endingTimes[i] = -1;
                    _machines[i] = "";
                }
            }
        }
    }
}