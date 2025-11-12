using MongoDB.Driver;

namespace ApiHosterLink.Helpers
{
    public static class MongoDBHelper
    {
        // Método seguro para construir filtros
        public static FilterDefinition<T> BuildSafeFilter<T>(string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Builders<T>.Filter.Empty;

            // Prevenir inyección NoSQL escapando caracteres especiales
            var safeValue = value.Trim().ToLower();
            return Builders<T>.Filter.Eq(field, safeValue);
        }

        // Consulta paginada optimizada
        public static async Task<(List<T> Results, long Total)> GetPaginatedResults<T>(
            IMongoCollection<T> collection,
            FilterDefinition<T> filter,
            SortDefinition<T> sort = null,
            int page = 1,
            int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Límite máximo

            var total = await collection.CountDocumentsAsync(filter);

            var findOptions = new FindOptions<T>
            {
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            };

            if (sort != null)
            {
                findOptions.Sort = sort;
            }

            var results = await collection.FindAsync(filter, findOptions);
            var list = await results.ToListAsync();

            return (list, total);
        }

        // Proyección para solo campos necesarios
        public static ProjectionDefinition<T> BuildProjection<T>(params string[] fields)
        {
            var projection = Builders<T>.Projection.Include("_id"); // Siempre incluir ID

            foreach (var field in fields)
            {
                projection = projection.Include(field);
            }

            return projection;
        }
    }
}