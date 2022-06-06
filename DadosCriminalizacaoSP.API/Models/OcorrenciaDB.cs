using MongoDB.Driver;

namespace DadosCriminalizacaoSP.API.Models
{
    public class OcorrenciaDB
    {
        public IMongoDatabase Database;

        public OcorrenciaDB(IConfiguration configuration) => 
            Database = new MongoClient(configuration["connection:mongo"])
                .GetDatabase(nameof(OcorrenciaDB));

        public IMongoCollection<OcorrenciaCol> OcorrenciasCol =>
            Database.GetCollection<OcorrenciaCol>(nameof(OcorrenciasCol));
    }
}
