using System.Collections.Generic;

namespace AElf.Common.FSM
{
    // ReSharper disable InconsistentNaming
    public class FSMStateData
    {
        /// <summary>
        /// Comes from.
        /// </summary>
        public FSM FSM { get; set; }

        /// <summary>
        /// Related behaviour.
        /// </summary>
        public FSMStateBehaviour StateBehaviour { get; set; }

        /// <summary>
        /// Current state.
        /// </summary>
        public int CurrentState { get; set; }

        /// <summary>
        /// Age of this state.
        /// </summary>
        public int Age { get; set; }

        public int AbsTime { get; set; }

        public int Progress { get; set; }
    }
}