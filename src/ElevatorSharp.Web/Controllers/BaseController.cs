﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using ElevatorSharp.Domain;
using ElevatorSharp.Domain.DataTransferObjects;
using ElevatorSharp.Web.ViewModels;

namespace ElevatorSharp.Web.Controllers
{
    public class BaseController : Controller
    {
        #region Helper Methods
        private static Skyscraper LoadSkyscraper()
        {
            var cache = MemoryCache.Default;
            if (cache.Contains("skyscraper"))
            {
                return (Skyscraper)cache.Get("skyscraper");
            }
            return null;
        }

        /// <summary>
        /// Transfer all required data from client.
        /// </summary>
        /// <param name="skyscraperDto"></param>
        /// <returns></returns>
        internal static Skyscraper SyncSkyscraper(SkyscraperDto skyscraperDto)
        {
            var skyscraper = LoadSkyscraper();
            for (var i = 0; i < skyscraperDto.Elevators.Count; i++)
            {
                skyscraper.Elevators[i].PressedFloors = skyscraperDto.Elevators[i].PressedFloors;
                skyscraper.Elevators[i].LoadFactor = skyscraperDto.Elevators[i].LoadFactor;
                skyscraper.Elevators[i].GoingDownIndicator = skyscraperDto.Elevators[i].GoingDownIndicator;
                skyscraper.Elevators[i].GoingUpIndicator = skyscraperDto.Elevators[i].GoingUpIndicator;
                skyscraper.Elevators[i].CurrentFloor = skyscraperDto.Elevators[i].CurrentFloor;
                skyscraper.Elevators[i].DestinationDirection = skyscraperDto.Elevators[i].DestinationDirection;
                
                skyscraper.Elevators[i].DestinationQueue.Clear();
                if (skyscraperDto.Elevators[i].DestinationQueue == null) continue;
                foreach (var floor in skyscraperDto.Elevators[i].DestinationQueue)
                    skyscraper.Elevators[i].DestinationQueue.Enqueue(floor);
            }

            return skyscraper;
        }

        /// <summary>
        /// Use ElevatorCommands for sending back to client.
        /// </summary>
        /// <param name="skyscraperDto"></param>
        /// <param name="skyscraper"></param>
        /// <returns></returns>
        internal static ElevatorCommands CreateElevatorCommands(SkyscraperDto skyscraperDto, Skyscraper skyscraper)
        {
            var elevatorCommands = new ElevatorCommands();
            var jumpQueueDestinations = skyscraper.Elevators[skyscraperDto.EventRaisedElevatorIndex].JumpQueueDestinations;
            var newDestinations = skyscraper.Elevators[skyscraperDto.EventRaisedElevatorIndex].NewDestinations;



            while (jumpQueueDestinations.Count > 0)
            {
                AddGoToFloorCommands(skyscraperDto, jumpQueueDestinations, elevatorCommands, true);
            }

            while (newDestinations.Count > 0)
            {
                AddGoToFloorCommands(skyscraperDto, newDestinations, elevatorCommands, false);
            }

            foreach (var elevator in skyscraper.Elevators)
            {
                if (elevator.StopElevator)
                {
                    elevatorCommands.StopElevators.Add(new StopElevator(elevator.Index));
                    elevator.StopElevator = false; // This is not synced so must be reset here
                }
                elevatorCommands.SetUpIndicators.Add(new SetIndicatorCommand(elevator.Index, elevator.GoingUpIndicator));
                elevatorCommands.SetDownIndicators.Add(new SetIndicatorCommand(elevator.Index, elevator.GoingDownIndicator));

                if (elevator.IsDestinationQueueModified)
                {
                    elevatorCommands.DestinationQueueCommands.Add(new DestinationQueueCommand
                    {
                        ElevatorIndex = elevator.Index,
                        DestinationQueue = elevator.DestinationQueue
                    });
                }
            }

            return elevatorCommands;
        }

        private static void AddGoToFloorCommands(SkyscraperDto skyscraperDto, Queue<int> queue, ElevatorCommands elevatorCommands, bool jumpQueue)
        {
            if (queue.Count > 0)
            {
                var destination = queue.Dequeue();
                var goToFloorCommand = new GoToFloorCommand(skyscraperDto.EventRaisedElevatorIndex, destination, jumpQueue);
                elevatorCommands.GoToFloors.Enqueue(goToFloorCommand); 
            }
        }
        #endregion
    }
}