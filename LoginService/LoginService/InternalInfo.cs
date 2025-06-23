namespace LoginService
{
    public static class InternalInfo
    {
        private static InternalInfoObject infoObject;

        public static string PodName
        {
            get
            {
                if (!infoObject.IsInitialized)
                    Initialize();

                return infoObject.PodName;
            }
        }

        public static string PodCreationTime
        {
            get
            {
                if (!infoObject.IsInitialized)
                    Initialize();

                return infoObject.PodCreationTime;
            }
        }

        public static string PodIP
        {
            get
            {
                if (!infoObject.IsInitialized)
                    Initialize();

                return infoObject.PodIP;
            }
        }

        public static string Version
        {
            get
            {
                if (!infoObject.IsInitialized)
                    Initialize();

                return infoObject.Version;
            }
        }

        private static void Initialize()
        {
            string? podName = Environment.GetEnvironmentVariable("POD_NAME");
            string? podIP = Environment.GetEnvironmentVariable("POD_IP");
            string? version = Environment.GetEnvironmentVariable("VERSION");

            infoObject = new InternalInfoObject(
                podName != null ? podName : "",
                podIP != null ? podIP : "",
                version != null ? version : ""
                );
        }

        private struct InternalInfoObject
        {
            private readonly bool _initialized = false;
            public bool IsInitialized { get => _initialized; }

            private readonly string _podName;
            public string PodName { get => _podName; }

            private readonly string _podCreationTime;
            public string PodCreationTime { get => _podCreationTime; }

            private readonly string _podIP;
            public string PodIP { get => _podIP; }

            private readonly string _version;
            public string Version { get => _version; }

            public InternalInfoObject(string podName, string podIP, string version)
            {
                _initialized = true;

                _podName = podName;
                _podCreationTime = DateTime.Now.ToString();
                _podIP = podIP;
                _version = version;
            }
        }
    }
}
