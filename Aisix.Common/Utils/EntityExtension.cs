namespace Aisix.Common.Utils
{
    public static class EntityExtension
    {
        private const string CreatedKey = "created";
        private const string LastModifiedKey = "last_modified";
        private const string CreatedIdKey = "created_id";
        private const string LastModifiedIdKey = "last_modified_id";
        private const string DeleteTimeKey = "delete_time";

        public static TSource ToCreate<TSource>(this TSource source, long currentUserId)
        {
            if (source == null)
            {
                return source;
            }

            var types = source.GetType();

            //if (types == null) return source;

            if (types.GetProperty(CreatedKey) != null)
            {
                types.GetProperty(CreatedKey)?.SetValue(source, DateTime.Now, null);
            }

            if (types.GetProperty(LastModifiedKey) != null)
            {
                types.GetProperty(LastModifiedKey)?.SetValue(source, DateTime.Now, null);
            }

            if (types.GetProperty(CreatedIdKey) != null)
            {
                types.GetProperty(CreatedIdKey)?.SetValue(source, currentUserId, null);
            }

            if (types.GetProperty(LastModifiedIdKey) != null)
            {
                types.GetProperty(LastModifiedIdKey)?.SetValue(source, currentUserId, null);
            }

            return source;
        }

        public static TSource ToUpdate<TSource>(this TSource source, long currentUserId)
        {
            var types = source.GetType();

            if (types.GetProperty(LastModifiedKey) != null)
            {
                types.GetProperty(LastModifiedKey)?.SetValue(source, DateTime.Now, null);
            }

            if (types.GetProperty(LastModifiedIdKey) != null)
            {
                types.GetProperty(LastModifiedIdKey)?.SetValue(source, currentUserId, null);
            }

            return source;
        }

        public static TSource ToDelete<TSource>(this TSource source, long currentUserId)
        {
            var types = source.GetType();

            if (types.GetProperty(DeleteTimeKey) != null)
            {
                types.GetProperty(DeleteTimeKey)?.SetValue(source, DateTime.Now, null);
            }

            if (types.GetProperty(LastModifiedKey) != null)
            {
                types.GetProperty(LastModifiedKey)?.SetValue(source, DateTime.Now, null);
            }

            if (types.GetProperty(LastModifiedIdKey) != null)
            {
                types.GetProperty(LastModifiedIdKey)?.SetValue(source, currentUserId, null);
            }

            return source;
        }
    }
}
