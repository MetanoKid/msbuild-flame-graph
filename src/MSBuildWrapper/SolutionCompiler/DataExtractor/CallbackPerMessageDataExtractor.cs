namespace MSBuildWrapper
{
    public class CallbackPerMessageDataExtractor : CompilationDataExtractor
    {
        public CallbackPerMessageDataExtractor(OnBuildMessage callback)
        {
            Logger = new AllMessagesToCallbackUILogger(callback);
        }
    }
}
