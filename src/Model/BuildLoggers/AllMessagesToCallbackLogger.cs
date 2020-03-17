namespace Model
{
    // logger that executes a callback when an event is logged
    public abstract class AllMessagesToCallbackLogger<T> : AllMessagesLogger
    {
        protected T m_onBuildEvent;

        public AllMessagesToCallbackLogger(T onBuildEvent)
        {
            m_onBuildEvent = onBuildEvent;
        }
    }
}
