﻿using TestSortingProblem.Structures;

namespace TestSortingProblem.Interfaces
{
    public interface IParser
    {
        Instance ParseData();
	    void FormatAndSaveResult(string[] result);
    }
}