using DadosCriminalizacaoSP.API.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DadosCriminalizacaoSP.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CidadesController : ControllerBase
    {
        private readonly OcorrenciaDB _ocorrenciaDB;
        public CidadesController(OcorrenciaDB ocorrenciaDB)
        {
            _ocorrenciaDB = ocorrenciaDB;
        }

        [HttpGet]
        public IEnumerable<string> Get() =>
            _ocorrenciaDB.OcorrenciasCol
                .Find(x => x.Cidade != null && x.Cidade != "")
                .ToEnumerable()
                .Select(x => x.Cidade)
                .Distinct()
                .OrderBy(x => x);
    }
}
