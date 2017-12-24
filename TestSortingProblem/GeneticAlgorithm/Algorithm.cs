﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestSortingProblem.Abstract;
using TestSortingProblem.Handlers;
using TestSortingProblem.Structures;
using System.Threading;

namespace TestSortingProblem.GeneticAlgorithm
{
	public class Algorithm : Ga
	{
		private const double Mortality = 0.5;
		private const int PopulationSize = 100;
		private const double MutationProbability = 0.01;
		private const double MaxNoChange = 10000;

		private bool abort = false;
		private Solution _solution;
		
		public Algorithm(Instance instance, ExecutionTime time) : base(instance, time)
		{
			_solution = new Solution(instance.Tests.Length);
		}

		public override Solution Solve(bool consolePrint)
		{
            Thread workerThread = new Thread(() => Start(consolePrint));
			workerThread.Start();

			SetAbortSignal();
			workerThread.Join();
			
			return _solution;
		}

		protected override void Start(bool consolePrint)
		{
			int i = 0;
			int fromLastChange = 0;
			int howManyDies = (int)(Mortality * PopulationSize);
			Genome lastBest = new Genome(Instance);
			RandomPopulation(PopulationSize);
			if(consolePrint)
				ConsoleHandler.PrintBestGenome(BestGenome, i);
			while (fromLastChange < MaxNoChange)
			{
				if(CheckAbortSignal())
					return;
				lastBest.Copy(BestGenome);
				//Parallel.For(0, howManyDies, ThreeTournament);	// Mortality determines how many times we should do the Tournaments
				DetermineBestFitness();
				if (!(BestGenome.Fitness < lastBest.Fitness)) continue;
				fromLastChange++;
				if(consolePrint)
					ConsoleHandler.PrintBestGenome(BestGenome, i);
			}
		}
		
		private void ThreeTournament(int index)
		{
			Random rnd = new Random(index);
			List<int> choices = new List<int>(3);
			while (true)
			{
				int randNum = rnd.Next(1, PopulationSize);
				if (choices.Contains(randNum)) continue;
				choices.Add(randNum);
				if(choices.Count == 3)
					break;
			}

			List<Genome> order = new List<Genome>(3);
			for (int i = 0; i < 3; i++)
			{
				Genome choice = Population[choices[i]];
				order.Add(choice);
			}

			Genome temp = new Genome(Instance);
			Order(order);
			temp.Copy(order[2]);

			Crossover(order[0], order[1], ref temp);
			for (int i = 0; i < temp.Size; i++)
			{
				if (Rand.NextDouble() < MutationProbability)
					Mutation(ref temp, i);
			}
			DetermineGenomeFitness(ref temp);
			order[2].Copy(temp);
		}

		protected override void UpdateResult()
		{
			_solution.SetTests(Instance.TestList);
			_solution.SetMachines(BestGenome.GetMachines());
			_solution.SetTimes(BestGenome.GetStartingTimes());
		}

		private bool CheckAbortSignal()
		{
			return abort;
		}

		private void SetAbortSignal()
		{
			int timeInMiliseconds = StringTime.Miliseconds(Time);
			if(timeInMiliseconds == 0)
				return;
			Thread.Sleep(timeInMiliseconds);
			abort = true;
		}
	}
}