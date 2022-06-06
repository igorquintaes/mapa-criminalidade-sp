using DadosCriminalizacaoSP.API.Enums;
using DadosCriminalizacaoSP.API.Models;
using DadosCriminalizacaoSP.API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DadosCriminalizacaoSP.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OcorrenciasController : ControllerBase
    {
        private readonly OcorrenciaDB _ocorrenciaDB;

        public OcorrenciasController(OcorrenciaDB ocorrenciaDB)
        {
            _ocorrenciaDB = ocorrenciaDB;
        }

        [HttpGet]
        public IEnumerable<Coordenada> Get(
            [FromQuery] DateTime? dataMinima,
            [FromQuery] DateTime? dataMaxima,
            [FromQuery] string? horaMinima,
            [FromQuery] string? horaMaxima,
            [FromQuery] IEnumerable<PeriodoOcorrencia>? periodos,
            [FromQuery] TipoOcorrencia? tipoOcorrencia,
            [FromQuery] string? cidade
            )
        {
            int? horaMinimaInt = null;
            if (horaMinima != null)
            {
                var horaMinimaDividida = horaMinima.Split(":");
                horaMinimaInt = int.Parse(horaMinimaDividida[0]) * 60 + int.Parse(horaMinimaDividida[1]);
            }

            int? horaMaximaInt = null;
            if (horaMaxima != null)
            {
                var horaMaximaDividida = horaMaxima.Split(":");
                horaMaximaInt = int.Parse(horaMaximaDividida[0]) * 60 + int.Parse(horaMaximaDividida[1]);
            }

            return _ocorrenciaDB.OcorrenciasCol
                .Find(x => x.Latitude != null
                        && x.Longitude != null
                        && (dataMinima == null || x.DataOcorrencia >= dataMinima)
                        && (dataMaxima == null || x.DataOcorrencia <= dataMaxima)
                        && (horaMinimaInt == null || x.HoraOcorrencia >= horaMinimaInt)
                        && (horaMaximaInt == null || x.HoraOcorrencia <= horaMaximaInt)
                        && (periodos == null || !periodos.Any() || periodos.Contains(x.PeriodoOcorrencia))
                        && (tipoOcorrencia == null || tipoOcorrencia == x.TipoOcorrencia)
                        && (cidade == null || cidade == x.Cidade))
                .Project(x => new Coordenada
                {
                    Latitude = x.Latitude.Value,
                    Longitude = x.Longitude.Value
                })
                .ToList();
        }
    }
}