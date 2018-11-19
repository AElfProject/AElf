namespace AElf.Common.FSM
{
    // ReSharper disable InconsistentNaming
    public class FSMStateData<T>
    {
        /// <summary>
        /// Comes from.
        /// </summary>
        public FSM<T> FSM { get; set; }

        /// <summary>
        /// Related behaviour.
        /// </summary>
        public FSMStateBehaviour<T> StateBehaviour { get; set; }

        /// <summary>
        /// Current state.
        /// </summary>
        public T CurrentState { get; set; }

        /// <summary>
        /// Age of this state.
        /// </summary>
        public double Age { get; set; }

        public double AbsTime { get; set; }

        public double Progress { get; set; }
    }
}