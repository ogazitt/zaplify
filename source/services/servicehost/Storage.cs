namespace BuiltSteady.Zaplify.ServiceHost
{
    // ****************************************************************************
    // static class for getting storage contexts
    // ****************************************************************************
    public static class Storage
    {
#if !DEBUG
        private static UserStorageContext staticUserContext;
#endif

        public static SuggestionsStorageContext NewSuggestionsContext
        {
            get { return new SuggestionsStorageContext(); }
        }

        public static UserStorageContext NewUserContext
        {
            get { return new UserStorageContext(); }
        }

        public static UserStorageContext StaticUserContext
        {   // use a static context to access static data (serving values out of EF cache)
            get
            {
#if DEBUG
                // if in a debug build, always go to the database
                return new UserStorageContext();
#else
                if (staticUserContext == null)
                {
                    staticUserContext = new UserStorageContext();
                }
                return staticUserContext;
#endif
            }
        }
    }

}