using System;
using System.Collections.Generic;
using Google.OrTools.Sat;

namespace MeetScheduler
{
    class AthleticMeetScheduler
    {
        public class Event
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        public class Participant
        {
            public string Name { get; set; }
            public List<int> EventIds { get; set; } // List of events the participant is part of
        }

        public static void Main()
        {
            // Define events (e.g., 100m, 200m, long jump)
            List<Event> events = new List<Event>
    {
        new Event { Name = "100m", Id = 0 },
        new Event { Name = "200m", Id = 1 },
        new Event { Name = "Long Jump", Id = 2 }
    };

            // Define participants and their event signups
            List<Participant> participants = new List<Participant>
    {
        new Participant { Name = "Alice", EventIds = new List<int> { 0, 2 } },
        new Participant { Name = "Bob", EventIds = new List<int> { 1, 2 } },
        new Participant { Name = "Charlie", EventIds = new List<int> { 0, 1 } }
    };

            // Number of time slots available
            int numSlots = 8;  // Use fewer time slots to simplify

            // Create a solver (CP-SAT solver)
            CpModel model = new CpModel();

            // Create variables: event[i] will be assigned to a time slot j
            IntVar[,] eventTimeSlots = new IntVar[events.Count, numSlots];
            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < numSlots; j++)
                {
                    eventTimeSlots[i, j] = model.NewBoolVar($"event_{i}_time_{j}");
                }
            }

            // Constraint 1: Each event must be assigned to exactly one time slot
            for (int i = 0; i < events.Count; i++)
            {
                List<IntVar> timeSlotsForEvent = new List<IntVar>();
                for (int j = 0; j < numSlots; j++)
                {
                    timeSlotsForEvent.Add(eventTimeSlots[i, j]);
                }

                // Add constraint: sum of time slots for this event should be exactly 1
                model.AddLinearConstraint(LinearExpr.Sum(timeSlotsForEvent), 1, 1);
            }

            // Simplified constraint: Ensure no two events overlap at the same time
            for (int t = 0; t < numSlots; t++)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    for (int j = i + 1; j < events.Count; j++)
                    {
                        // Add constraint to ensure events i and j do not overlap at time slot t
                        model.AddLinearConstraint(
                            eventTimeSlots[i, t] + eventTimeSlots[j, t], 0, 1);
                    }
                }
            }

            // Solve the problem
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status == CpSolverStatus.Optimal)
            {
                // Output the solution
                Console.WriteLine("Schedule:");
                for (int i = 0; i < events.Count; i++)
                {
                    for (int j = 0; j < numSlots; j++)
                    {
                        if (solver.Value(eventTimeSlots[i, j]) == 1)
                        {
                            Console.WriteLine($"{events[i].Name} at time slot {j}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No optimal solution found.");
            }
        }
    }
}
