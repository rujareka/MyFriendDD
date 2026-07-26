using UnityEngine;

namespace MyFriendDD.Debugging
{
    /// <summary>
    /// OVRHaptics가 OpenXR 액션셋이 세션에 attach되기 전에 샘플레이트를 조회하면서
    /// 찍는 무해한 XR_ERROR_ACTIONSET_NOT_ATTACHED 경고를 콘솔에서 감춘다.
    /// </summary>
    public static class OVRWarningLogFilter
    {
        private const string FilteredSubstring = "XR_ERROR_ACTIONSET_NOT_ATTACHED";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Debug.unityLogger.logHandler = new FilteringLogHandler(Debug.unityLogger.logHandler);
        }

        private class FilteringLogHandler : ILogHandler
        {
            private readonly ILogHandler _inner;

            public FilteringLogHandler(ILogHandler inner)
            {
                _inner = inner;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Warning && format != null && format.Contains(FilteredSubstring))
                {
                    return;
                }

                _inner.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, Object context)
            {
                _inner.LogException(exception, context);
            }
        }
    }
}
