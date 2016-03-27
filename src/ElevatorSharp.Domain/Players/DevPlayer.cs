﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ElevatorSharp.Domain.Players
{
    public class DevPlayer : IPlayer
    {
        #region Implementation of IPlayer
        public void Init(IList<IElevator> elevators, IList<IFloor> floors)
        {
            foreach (var elevator in elevators)
            {
                elevator.Idle += Elevator_Idle; ;
                elevator.FloorButtonPressed += Elevator_FloorButtonPressed;
                elevator.StoppedAtFloor += Elevator_StoppedAtFloor;
            }

            foreach (var floor in floors)
            {
                floor.UpButtonPressed += Floor_UpButtonPressed;
                floor.DownButtonPressed += Floor_DownButtonPressed;
            }
        }

        public void Update(IList<IElevator> elevators, IList<IFloor> floors)
        {
            // We normally don't need to do anything here
        }
        #endregion
        
        #region Event Handlers
        private void Floor_DownButtonPressed(object sender, IList<IElevator> elevators)
        {
            var floor = (Floor)sender;

            // Just pick first elevator to start with and go to the floor the button was pressed.
            // Could check which floor each elevator is currently on and which direction they are travelling?

            // grab the first elevator that is going up
            var elevator = elevators.FirstOrDefault(e => e.DestinationDirection == ElevatorDirection.Stopped);
            elevator?.GoToFloor(floor.FloorNum);
        }

        private void Floor_UpButtonPressed(object sender, IList<IElevator> elevators)
        {
            var floor = (Floor)sender;

            // Just pick first elevator to start with and go to the floor the button was pressed.
            // Could check which floor each elevator is currently on and which direction they are travelling?

            // grab the first elevator that is going up
            var elevator = elevators.FirstOrDefault(e => e.DestinationDirection == ElevatorDirection.Stopped);
            elevator?.GoToFloor(floor.FloorNum);
        }

        private void Elevator_Idle(object sender, EventArgs e)
        {
            var elevator = (Elevator)sender;

            //elevator.GoToFloor(0);
            //elevator.GoToFloor(1);
            //elevator.GoToFloor(2);
        }

        private void Elevator_FloorButtonPressed(object sender, int e)
        {
            var elevator = (IElevator)sender;

            // Check if we're not already going to that floor
            if (!elevator.DestinationQueue.Contains(e))
            {
                // Go to the floor this passenger wants to go
                elevator.GoToFloor(e);
            }
        }

        private void Elevator_StoppedAtFloor(object sender, int e)
        {
            var elevator = (Elevator)sender;

            // remember that elevator.GoingUp(and Down)Indicator influences if passengers get on.

            // TODO: Do something here?
            // A passenger will only get on if the
            // indicator was pointing in the direction in which
            // they want to travel.
        }
        #endregion




    }
}