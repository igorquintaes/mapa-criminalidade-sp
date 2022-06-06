using DadosCriminalizacaoSP.API.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DadosCriminalizacaoSP.API.Models
{
    public class OcorrenciaCol
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public short Ano { get; init; }
        public int NumeroBoletim { get; init; }
        public DateTime DataOcorrencia { get; init; }
        public int? HoraOcorrencia { get; init; }
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public string Cidade { get; init; }
        public TipoOcorrencia TipoOcorrencia { get; init; }
        public PeriodoOcorrencia PeriodoOcorrencia { get; set; }
    }
}
