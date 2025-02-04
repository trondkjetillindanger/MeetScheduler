using System;
using System.Collections.Generic;
using Google.OrTools.Sat;

namespace MeetScheduler
{
    public class AthleticMeetScheduler
    {

        public const int MinutesPerSlot = 5;
        public static DateTime meetDateTime = new DateTime(2025, 2, 4, 12, 0, 0);

        public static void Main()
        {
            // Define events (100m, 200m, Long Jump)
            List<Event> events = new List<Event>
            {
                new Event { Name = "100m", Id = 0, DurationInSlots = 1, Area = "Track" },
                new Event { Name = "200m", Id = 1, DurationInSlots = 2, Area = "Track" },
                new Event { Name = "Long Jump MS", Id = 2, DurationInSlots = 4 , Area = "Field"},
                new Event { Name = "Long Jump KS", Id = 3, DurationInSlots = 4 , Area = "Field"}
            };

            // Define participants and their event signups
            List<Participant> participants = new List<Participant>
            {
                new Participant { Name = "Alice", EventIds = new List<int> { 0, 2 } },
                new Participant { Name = "Bob", EventIds = new List<int> { 1, 2 } },
                new Participant { Name = "Charlie", EventIds = new List<int> { 0, 1 } },
                new Participant { Name = "Lisa", EventIds = new List<int> { 1, 3 } }
            };

            // Number of time slots available
            int numSlots = 14;

            var (eventTimeSlots, solver, status) = GetMeetSchedule(events, participants, numSlots);
            PrintResult(eventTimeSlots, solver, status, events, participants, numSlots);

        }

        public static string SlotNoToTimeString(int slotNo)
        {
            int minutes = slotNo * MinutesPerSlot;
            return meetDateTime.AddMinutes(minutes).ToString("HH:mm");
        } 

        public static void PrintResult(IntVar[,] eventTimeSlots, CpSolver solver, CpSolverStatus status, List<Event> events, List<Participant> participants, int numSlots)
        {
            Console.WriteLine(solver.SolutionInfo());
            if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
            {
                Console.WriteLine("Optimal Schedule:");
                for (int i = 0; i < events.Count; i++)
                {
                    for (int t = 0; t < numSlots; t++)
                    {
                        if (solver.Value(eventTimeSlots[i, t]) == 1)
                        {
                            Console.WriteLine($"{events[i].Name} scheduled from time slot {SlotNoToTimeString(t)} to {SlotNoToTimeString(t + events[i].DurationInSlots - 1)}");
                        }
                    }
                }

                // Print the time schedule for each participant
                foreach (var participant in participants)
                {
                    Console.WriteLine($"\n{participant.Name}'s Schedule:");
                    foreach (var eventId in participant.EventIds)
                    {
                        for (int t = 0; t < numSlots; t++)
                        {
                            if (solver.Value(eventTimeSlots[eventId, t]) == 1)
                            {
                                Console.WriteLine($"{events[eventId].Name} scheduled from time slot {SlotNoToTimeString(t)} to {SlotNoToTimeString(t + events[eventId].DurationInSlots - 1)}");
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No solution found.");
            }

        }

        public static (IntVar[,],CpSolver,CpSolverStatus) GetMeetSchedule(List<Event> events, List<Participant> participants, int numSlots)
        {
            // Create a solver (CP-SAT solver)
            CpModel model = new CpModel();

            // Decision variables: event[i, t] is 1 if event i starts at time slot t
            IntVar[,] eventTimeSlots = new IntVar[events.Count, numSlots];
            for (int i = 0; i < events.Count; i++)
            {
                for (int t = 0; t < numSlots; t++)
                {
                    eventTimeSlots[i, t] = model.NewBoolVar($"event_{i}_starts_at_{t}");
                }
            }

            AddConstraintNotSameEventInSameAreaSimultaneously(model, eventTimeSlots, events, numSlots);
            AddConstraintNoOverlappingEventsForParticipants(model, eventTimeSlots, events, participants, numSlots);
            AddConstraintSequentialEventStartTimes(model, eventTimeSlots, events, numSlots);
            AddConstraintEventExactlyOnce(model, eventTimeSlots, events, numSlots);

            // Solve the problem
            Console.WriteLine("Starting to solve...");
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);
            return (eventTimeSlots, solver, status);
        }

        private static void AddConstraintEventExactlyOnce(CpModel model, IntVar[,] eventTimeSlots, List<Event> events, int numSlots)
        {
            // Each event must be scheduled exactly once
            for (int i = 0; i < events.Count; i++)
            {
                List<IntVar> timeSlotsForEvent = new List<IntVar>();
                for (int t = 0; t < numSlots; t++)
                {
                    timeSlotsForEvent.Add(eventTimeSlots[i, t]);
                }
                model.Add(LinearExpr.Sum(timeSlotsForEvent) == 1);  // Each event starts exactly once
            }
        }

        private static void AddConstraintNotSameEventInSameAreaSimultaneously(CpModel model, IntVar[,] eventTimeSlots, List<Event> events, int numSlots)
        {
            // Ensure that events do not overlap in the same time slot for events in the same area
            for (int t = 0; t < numSlots; t++)
            {
                foreach (var areaGroup in events.GroupBy(e => e.Area))
                {
                    List<IntVar> timeSlotUsage = new List<IntVar>();

                    foreach (var ev in areaGroup)
                    {
                        timeSlotUsage.Add(eventTimeSlots[ev.Id, t]);
                    }

                    // Ensure that at most one event per area is scheduled at the same time
                    model.Add(LinearExpr.Sum(timeSlotUsage) <= 1);
                }
            }
        }

        private static void AddConstraintNoOverlappingEventsForParticipants(CpModel model, IntVar[,] eventTimeSlots, List<Event> events, List<Participant> participants, int numSlots)
        {
            foreach (var participant in participants)
            {
                for (int i = 0; i < participant.EventIds.Count; i++)
                {
                    for (int j = i + 1; j < participant.EventIds.Count; j++)
                    {
                        int event1Id = participant.EventIds[i];
                        int event2Id = participant.EventIds[j];

                        for (int t1 = 0; t1 <= numSlots - events[event1Id].DurationInSlots; t1++)
                        {
                            for (int t2 = 0; t2 <= numSlots - events[event2Id].DurationInSlots; t2++)
                            {
                                // Check if event 1 and event 2 overlap
                                if (t1 + events[event1Id].DurationInSlots > t2 && t2 + events[event2Id].DurationInSlots > t1)
                                {
                                    Console.WriteLine($"Checking overlap for {events[event1Id].Name} (starts at {t1}) and {events[event2Id].Name} (starts at {t2})");

                                    // We need to enforce that either event1 finishes before event2 starts, or event2 finishes before event1 starts
                                    var event1BeforeEvent2 = model.NewBoolVar($"event1_{event1Id}_before_event2_{event2Id}");
                                    var event2BeforeEvent1 = model.NewBoolVar($"event2_{event2Id}_before_event1_{event1Id}");

                                    // Event 1 finishes before event 2 starts
                                    model.AddLinearConstraint(
                                        LinearExpr.Sum(Enumerable.Range(t1, events[event1Id].DurationInSlots).Select(slot => eventTimeSlots[event1Id, slot])),
                                        0, 0).OnlyEnforceIf(event1BeforeEvent2); // Enforce when event1BeforeEvent2 is true

                                    // Event 2 finishes before event 1 starts
                                    model.AddLinearConstraint(
                                        LinearExpr.Sum(Enumerable.Range(t2, events[event2Id].DurationInSlots).Select(slot => eventTimeSlots[event2Id, slot])),
                                        0, 0).OnlyEnforceIf(event2BeforeEvent1); // Enforce when event2BeforeEvent1 is true

                                    // Ensure that at least one of the two conditions is true (either event1BeforeEvent2 or event2BeforeEvent1)
                                    model.AddLinearConstraint(
                                        event1BeforeEvent2 + event2BeforeEvent1, 1, 1); // Enforces at least one of them is true
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AddConstraintSequentialEventStartTimes(CpModel model, IntVar[,] eventTimeSlots, List<Event> events, int numSlots)
        {
            var eventTypes = events.GroupBy(e => e.EventType); // Group events by type (e.g., 100m, Long Jump)

            foreach (var eventType in eventTypes)
            {
                List<int> eventIds = eventType.Select(e => e.Id).ToList(); // Get event IDs for this type

                for (int i = 0; i < eventIds.Count - 1; i++)
                {
                    int event1Id = eventIds[i];
                    int event2Id = eventIds[i + 1];

                    for (int t = 0; t < numSlots; t++)
                    {
                        int nextStartTime = t + events[event1Id].DurationInSlots;

                        if (nextStartTime < numSlots - events[event2Id].DurationInSlots)
                        {
                            // If event1 is scheduled at t, event2 must be scheduled at nextStartTime
                            model.AddImplication((ILiteral)eventTimeSlots[event1Id, t], (ILiteral)eventTimeSlots[event2Id, nextStartTime]);
                        }
                        else
                        {
                            model.Add(eventTimeSlots[event1Id, t] == 0);
                            // If event1 starts too late, then either event1 or event2 must NOT be scheduled
                        }
                    }
                }
            }
        }




        // model.AddImplication(eventTimeSlots[event1Id, t] == 1, eventTimeSlots[event2Id, t + events[event1Id].DurationInSlots] == 1);


    }
}
