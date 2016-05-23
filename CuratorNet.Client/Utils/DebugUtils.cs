namespace Org.Apache.CuratorNet.Client.Utils
{
    public class DebugUtils
    {
        public static readonly string PROPERTY_LOG_EVENTS = "curator-log-events";
        public static readonly string PROPERTY_DONT_LOG_CONNECTION_ISSUES = "curator-dont-log-connection-problems";
        public static readonly string PROPERTY_LOG_ONLY_FIRST_CONNECTION_ISSUE_AS_ERROR_LEVEL = "curator-log-only-first-connection-issue-as-error-level";            
        public static readonly string PROPERTY_RETRY_FAILED_TESTS = "curator-retry-failed-tests";

        private DebugUtils()
        {
        }
    }

}
