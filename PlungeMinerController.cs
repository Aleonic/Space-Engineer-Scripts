using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        // Names for important objects:
        const string startObjectName = "Rotating Light Starter";
        const string resetObjectName = "Inset Light Reset";

        const string sidePistonsGroupName = "Pistons Side";
        const string miningPistonsGroupName = "Drill Pistons";

        const string basePistonName = "Piston Base";

        const string baseRotorName = "Advanced Rotor Base";
        const string drillRotorName = "Advanced Rotor Drill";

        const float drillingSpeed = 0.1f;
        const float returnSpeed = 3.0f;

        // Actual Objects
        IMyFunctionalBlock startObject = null;
        IMyFunctionalBlock resetObject = null;

        List<IMyPistonBase> sidePistonsGroup = null;
        List<IMyPistonBase> miningPistonsGroup = null;

        IMyPistonBase basePiston = null;

        IMyMotorAdvancedStator baseRotor = null;
        IMyMotorAdvancedStator drillRotor = null;

        List<IMyShipDrill> drills = null;
        List<IMyLandingGear> magnets = null;

        public Program()
        {
            // Set initial state
            Storage = "Idle";

            //Set refresh rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // Get trigger objects
            startObject = (IMyFunctionalBlock)GridTerminalSystem.GetBlockWithName(startObjectName);
            resetObject = (IMyFunctionalBlock)GridTerminalSystem.GetBlockWithName(resetObjectName);

            //Get piston groups
            sidePistonsGroup = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlockGroupWithName(sidePistonsGroupName).GetBlocksOfType<IMyPistonBase>(sidePistonsGroup);

            miningPistonsGroup = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlockGroupWithName(miningPistonsGroupName).GetBlocksOfType<IMyPistonBase>(miningPistonsGroup);

            // Get base Piston
            basePiston = (IMyExtendedPistonBase)GridTerminalSystem.GetBlockWithName(basePistonName);

            // Get individual Rotors
            baseRotor = (IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName(baseRotorName);
            drillRotor = (IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName(drillRotorName);

            // Get generally used blocks
            drills = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);

            magnets = new List<IMyLandingGear>();
            GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(magnets);
        }

        public bool IsResetActive()
        {
            if(resetObject.Enabled == true)
            {
                SaveState("Reset");
                return true;
            }
            return false;
        }


        public bool LockProcedure()
        {
            bool allReachedMaxPosition = true;
            foreach (IMyPistonBase piston in sidePistonsGroup)
            {
                if (piston.Velocity <= 0)
                {
                    piston.Reverse();
                }

                if (piston.CurrentPosition != piston.HighestPosition)
                {
                    allReachedMaxPosition = false;
                }
            }

            if (allReachedMaxPosition)
            {
                foreach (IMyLandingGear magnet in magnets)
                {
                    magnet.Lock();
                }
                return true;
            }

            return false;
        }

        public bool UnlockProcedure()
        {
            foreach (IMyLandingGear magnet in magnets)
            {
                magnet.Unlock();
            }

            bool allReachedMinPosition = true;
            foreach (IMyPistonBase piston in sidePistonsGroup)
            {
                if (piston.Velocity >= 0)
                {
                    piston.Reverse();
                }

                if (piston.CurrentPosition != piston.LowestPosition)
                {
                    allReachedMinPosition = false;
                }
            }

            return allReachedMinPosition ? true : false;
        }

        public bool RiseProcedure()
        {
            if(baseRotor.TargetVelocityRPM <= 0)
            {
                baseRotor.TargetVelocityRPM = -baseRotor.TargetVelocityRPM;

                drillRotor.Enabled = true;

                foreach (IMyShipDrill drill in drills)
                {
                    if(!drill.Enabled)
                    {
                        drill.Enabled = true;
                    }
                }
            }

            if(baseRotor.Angle >= 1.55f)
            {
                return true;
            }

            return false;
        }

        public bool LowerProcedure()
        {
            if (baseRotor.TargetVelocityRPM >= 0)
            {
                baseRotor.TargetVelocityRPM = -baseRotor.TargetVelocityRPM;

                drillRotor.Enabled = false;

                foreach (IMyShipDrill drill in drills)
                {
                    if (drill.Enabled)
                    {
                        drill.Enabled = false;
                    }
                }
            }

            if (baseRotor.Angle <= 0.05f)
            {
                return true;
            }

            return false;
        }

        public bool MiningProcedure()
        {           
            // Move drill pistons
            bool drillPistonsComplete = true;
            foreach (IMyPistonBase piston in miningPistonsGroup)
            {
                if(piston.Velocity <= 0)
                {
                    piston.Velocity = drillingSpeed;
                }
                if(piston.CurrentPosition != piston.HighestPosition)
                {
                    drillPistonsComplete = false;
                    break;
                }
            }

            // Wait until all pistons have been moved.
            if (!drillPistonsComplete) return false;

            // Move base piston
            if (basePiston.Velocity >= 0)
            {
                basePiston.Velocity = -drillingSpeed;
            }
            if (basePiston.CurrentPosition == basePiston.LowestPosition)
            {
                return true;
            }

            return false;
        }

        public bool ReturnDrillProcedure()
        {
            // Move drill pistons back into standard position.

            if(basePiston.Velocity <= 0)
            {
                basePiston.Velocity = returnSpeed;
            }

            foreach (IMyPistonBase piston in miningPistonsGroup)
            {
                if (piston.Velocity >= 0)
                {
                    piston.Velocity = -returnSpeed;
                }
            }

            if(basePiston.CurrentPosition != basePiston.HighestPosition)
            {
                return false;
            }
            foreach(IMyPistonBase piston in miningPistonsGroup)
            {
                if (piston.CurrentPosition != piston.LowestPosition)
                {
                    return false;
                }
            }

            return true;
        }

        public void SaveState(string state)
        {

            /* -------------Save State Logic-------------
             * There will be 5 Default states in total:
             *  + Idle - Starting stage (Assumed parked)
             *  + Locking - The magnetic locks will connect to the ground to prevent the vehicle from moving.
             *  + Rising - The base rotor will rotate to the mining position. The drills and drill rotor would be activated.
             *  + Mining - The mining pistons will be activated one by one. Order will be decided by a group/collection. Then finally the base piston will be activated.
             *  + Reset - Reset the entire process by going backwards in the order. (More detail below)
             *  
             *  
             *  -------------Reset Logic-------------
             *  This state is intended to be activated at any time. This state should be able to reset the entire process at any point.
             *  The Reset states will be completed in the following order:
             *      + Return Drill - Turn all Mining Pistons to negative velocities and base piston to a positive velocity. Check for final states.
             *      + Lowering - Rotate base rotor to mobile state. Deactivate drills and drill rotor.
             *      + Unlocking - Remove magnetic locks and lift side pistons.
             *      + Idle
             */

            Storage = state;
            Echo(state);
        }
        public string LoadState()
        {
            return Storage;
        }

        public void DebugDisplay()
        {
            // Function created to test values output if necessary. Will likely be left blank.

            return;
        }

        public void Main()
        {
            DebugDisplay();
            switch (LoadState())
            {
                // Standard cases
                case "Idle":
                    if (resetObject.Enabled)
                    {
                        resetObject.Enabled = false;
                    }

                    if (startObject.Enabled)
                    {
                        SaveState("Locking");
                    }
                    break;

                case "Locking":
                    if (!IsResetActive())
                    {
                        if (LockProcedure())
                        {
                            SaveState("Rising");
                        }
                    }
                    break;

                case "Rising":
                    if (!IsResetActive())
                    {
                        if (RiseProcedure())
                        {
                            SaveState("Mining");
                        }
                    }
                    break;

                case "Mining":
                    if (!IsResetActive())
                    {
                        if (MiningProcedure())
                        {
                            SaveState("Reset");
                        }
                    }
                    break;

                // Reset cases
                case "Reset":
                    resetObject.Enabled = true;
                    SaveState("Return Drill");
                    break;

                case "Return Drill":
                    if (ReturnDrillProcedure())
                    {
                        SaveState("Lowering");
                    }
                    break;

                case "Lowering":
                    if (LowerProcedure())
                    {
                        SaveState("Unlocking");
                    }
                    break;

                case "Unlocking":
                    if (UnlockProcedure())
                    {
                        startObject.Enabled = false;
                        resetObject.Enabled = false;
                        SaveState("Idle");
                    }
                    break;
                default:
                    Echo("Currently at default.");
                    Echo("Current state is:");
                    Echo(LoadState());
                    break;
            }
        }
    }
}
