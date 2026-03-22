using System;
using System.Collections.Generic;
using OpenFFBoardPlugin.DTO;

namespace OpenFFBoardPlugin.Utils
{
    internal class ProfileToCommandConverter
    {
        internal OpenFFBoard.Commands.FX fx = null;
        internal OpenFFBoard.Commands.FX axis = null;

        /**
         * Let's map the JSON "Cls" key to the actual command class types.
         */
        private static Func<bool> MapCommand(ProfileData data, OpenFFBoard.Board board)
        {
            if (data.Cls == "fx")
            {
                switch (data.Cmd)
                {
                    case "filterCfFreq":
                        return () => board.FX.SetFiltercffreq((ushort)data.Value);
                    case "filterCfQ":
                        return () => board.FX.SetFiltercfq((byte)data.Value);
                    case "spring":
                        return () => board.FX.SetSpring((byte)data.Value);
                    case "friction":
                        return () => board.FX.SetFriction((byte)data.Value);
                    case "damper":
                        return () => board.FX.SetDamper((byte)data.Value);
                    case "inertia":
                        return () => board.FX.SetInertia((byte)data.Value);
                    default:
                        SimHub.Logging.Current.Error($"Unknown FX instance: {data.Instance}");
                        break;
                }
            }

            if (data.Cls == "axis")
            {
                switch (data.Cmd)
                {
                    case "power":
                        return () => board.Axis.SetPower((ushort)data.Value);
                    case "degrees":
                        return () => board.Axis.SetDegrees((ushort)data.Value);
                    case "fxratio":
                        return () => board.Axis.SetFxratio((byte)data.Value);
                    case "esgain":
                        return () => board.Axis.SetEsgain((byte)data.Value);
                    case "idlespring":
                        return () => board.Axis.SetIdlespring((byte)data.Value);
                    case "axisdamper":
                        return () => board.Axis.SetAxisdamper((byte)data.Value);
                    case "axisfriction":
                        return () => board.Axis.SetAxisfriction((byte)data.Value);
                    // The descriptor for axisinertia is wrong in v4 of the lib, so we cannot use this right now.
                    /* case "axisinertia":
                        return () => board.Axis.SetAxisinertia((byte)data.Value);*/
                    default:
                        SimHub.Logging.Current.Error($"Unknown AXIS instance: {data.Instance}");
                        break;
                }
            }

            return null;
        }

        /**
         * Returns a list of commands to execute based on the provided profile.
         * The sequence is respecting the order saved in the profile.
         */
        public static List<Func<bool>> ConvertProfileToCommands(Profile profile, OpenFFBoard.Board board)
        {
            List<Func<bool>> cmdsToReturn = new List<Func<bool>>();
            profile.Data.ForEach(data =>
            {
                Func<bool> cmd = MapCommand(data, board);
                if (cmd == null) {
                    SimHub.Logging.Current.Error($"Failed to map command for {data.Fullname} - {data.Cls} - {data.Cmd}");
                    return;
                }

                cmdsToReturn.Add(cmd);
            });

            return cmdsToReturn;
        }
    }
}
