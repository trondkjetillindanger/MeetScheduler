using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.OrTools.Sat;

namespace MeetScheduler
{
    public class SolutionCollector : CpSolverSolutionCallback
    {
        private readonly IntVar[,] _eventTimeSlots;
        private readonly CpSolver _solver;
        private readonly List<Event> _events;
        private readonly int _numSlots;
        private int _solutionCount;

        public SolutionCollector(IntVar[,] eventTimeSlots, CpSolver solver, List<Event> events, int numSlots)
        {
            _eventTimeSlots = eventTimeSlots;
            _solver = solver;
            _events = events;
            _numSlots = numSlots;
            _solutionCount = 0;
        }

        public override void OnSolutionCallback()
        {
            _solutionCount++;
            Console.WriteLine($"Solution {_solutionCount}:");
            for (int i = 0; i < _events.Count; i++)
            {
                for (int t = 0; t < _numSlots; t++)
                {
                    if (_solver.Value(_eventTimeSlots[i, t]) == 1)
                    {
                        Console.WriteLine($"{_events[i].Name} scheduled from time slot {t} to {t + _events[i].DurationInSlots - 1}");
                    }
                }
            }
        }

        public void PrintSolutions()
        {
            if (_solutionCount == 0)
            {
                Console.WriteLine("No solution found.");
            }
        }
    }
}
