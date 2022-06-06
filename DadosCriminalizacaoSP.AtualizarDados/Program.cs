using ClosedXML.Excel;
using DadosCriminalizacaoSP.API.Enums;
using DadosCriminalizacaoSP.API.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Globalization;
using System.Text.Json;

namespace DadosCriminalizacaoSP.AtualizarDados
{
    public class Program
    {
        private static IConfiguration configuration;
        private static readonly HttpClient HttpClient = new ();
        private static OcorrenciaDB context;

        public static void Main()
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            configuration = builder.Build();

            context = new OcorrenciaDB(configuration); 
            var ocorrenciasSalvas = context.OcorrenciasCol.Find(_ => true).ToList();

            Console.WriteLine("Insira o caminho do arquivo");
            var caminho = Console.ReadLine();
            Console.Clear();

            if (!File.Exists(caminho))
            {
                Console.WriteLine("Caminho de arquivo inválido.");
                return;
            }

            if (Path.GetExtension(caminho) != ".xlsx")
            {
                Console.WriteLine("Arquivo com extensão inválida.");
                return;
            }

            var wb = new XLWorkbook(caminho);
            var ws = wb.Worksheet(1);

            var linha = ws.FirstRow().RowBelow();

            const int colunaAno = 1;
            const int colunaNumeroBoletim = 2;
            const int colunaDataOcorrencia = 6;
            const int colunaHoraOcorrencia = 7;
            const int colunaPeriodoOcorrencia = 8;
            const int colunaEnderecoLogradouro = 14;
            const int colunaEnderecoNumero = 15;
            const int colunaEnderecoBairro = 16;
            const int colunaEnderecoCidade = 17;
            const int colunaLatitude = 19;
            const int colunaLongitude = 20;

            var periodoDicionario = new Dictionary<string, PeriodoOcorrencia>
            {
                { "PELA MANHÃ", PeriodoOcorrencia.Manha },
                { "A TARDE", PeriodoOcorrencia.Tarde },
                { "A NOITE", PeriodoOcorrencia.Noite },
                { "DE MADRUGADA", PeriodoOcorrencia.Madrugada },
                { "EM HORA INCERTA", PeriodoOcorrencia.Incerto }
            };

            var apiKey = configuration["api:geolocalizacao"];

            while (!linha.IsEmpty())
            {
                var estaValido = short.TryParse(
                    linha.Cell(colunaAno).GetFormattedString(),
                    out var ano);
                estaValido &= int.TryParse(
                    linha.Cell(colunaNumeroBoletim).GetFormattedString(),
                    out var numeroBoletim);

                if (!estaValido)
                    throw new ArgumentException($"linha com valores inválidos! linha {linha.RowNumber()}");

                var cidade = linha.Cell(colunaEnderecoCidade).GetFormattedString()
                    ?? throw new ArgumentException($"nome de cidade nulo! linha {linha.RowNumber()}");

                if (ocorrenciasSalvas.Any(x => 
                    ano == x.Ano && 
                    numeroBoletim == x.NumeroBoletim && 
                    cidade == x.Cidade))
                {
                    linha = linha.RowBelow();
                    continue;
                }

                decimal? latitudeValor = default;
                decimal? longitudeValor = default;
                var latitude = linha.Cell(colunaLatitude).GetFormattedString();
                var longitude = linha.Cell(colunaLongitude).GetFormattedString();

                if (string.IsNullOrWhiteSpace(latitude) ||
                    string.IsNullOrWhiteSpace(longitude))
                {
                    var logradouro = linha.Cell(colunaEnderecoLogradouro).GetFormattedString();
                    var numero = linha.Cell(colunaEnderecoNumero).GetFormattedString();
                    var bairro = linha.Cell(colunaEnderecoBairro).GetFormattedString();

                    if (!string.IsNullOrWhiteSpace(logradouro) &&
                        !string.IsNullOrWhiteSpace(numero) &&
                        !string.IsNullOrWhiteSpace(bairro) &&
                        !string.IsNullOrWhiteSpace(cidade))
                    {
                        var enderecoCompleto = Uri.EscapeDataString($"{logradouro}, {numero}, {bairro}, {cidade.Replace("S.", "SÃO ")} - SP - BRASIL");
                        var link = $"http://api.positionstack.com/v1/forward?access_key={apiKey}&query={enderecoCompleto}&limit=1";
                        var response = HttpClient.GetAsync(link).GetAwaiter().GetResult();
                        var textoRespostas = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception("erro ao puxar dados do mapa! " + textoRespostas);
                        }

                        if (textoRespostas != "{\"data\":[[]]}")
                        {
                            var respostaApi = JsonSerializer.Deserialize<RespostaApiObterCoordenadas>(textoRespostas, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (respostaApi.Data.Count > 0)
                            {
                                var result = respostaApi.Data.First();
                                latitudeValor = result.Latitude;
                                longitudeValor = result.Longitude;
                            }
                        }
                    }

                }

                if (latitudeValor == null && 
                    longitudeValor == null && 
                    !string.IsNullOrWhiteSpace(latitude) &&
                    !string.IsNullOrWhiteSpace(longitude))
                {
                    latitude = string.Join("", latitude.Where(x => char.IsNumber(x) || x == '-'));
                    var latitudeSkipValue = latitude.Contains('-') ? 3 : 2;
                    latitude = string.Concat(latitude.AsSpan(0, latitudeSkipValue), ",", latitude[latitudeSkipValue..]);
                    latitudeValor = decimal.Parse(latitude, CultureInfo.GetCultureInfo("pt-BR"));

                    longitude = string.Join("", longitude.Where(x => char.IsNumber(x) || x == '-'));
                    var longitudeSkipValue = longitude.Contains('-') ? 3 : 2;
                    longitude = string.Concat(longitude.AsSpan(0, longitudeSkipValue), ",", longitude[longitudeSkipValue..]);
                    longitudeValor = decimal.Parse(longitude, CultureInfo.GetCultureInfo("pt-BR"));
                }

                var data = DateTime.ParseExact(
                    linha.Cell(colunaDataOcorrencia).GetFormattedString(),
                    "d/M/yyyy",
                    new CultureInfo("pt-BR"),
                    DateTimeStyles.None);

                int? hora = null;
                if (!string.IsNullOrWhiteSpace(linha.Cell(colunaHoraOcorrencia).GetFormattedString()))
                {
                    var horaOcorrenciaDividida = linha.Cell(colunaHoraOcorrencia).GetFormattedString().Split(":");
                    hora = int.Parse(horaOcorrenciaDividida[0]) * 60 + int.Parse(horaOcorrenciaDividida[1]);
                }

                var ocorrencia = new OcorrenciaCol
                {
                    Ano = ano,
                    Cidade = cidade,
                    DataOcorrencia = data,
                    HoraOcorrencia = hora,
                    PeriodoOcorrencia = periodoDicionario[linha.Cell(colunaPeriodoOcorrencia).GetFormattedString()
                        ?? throw new ArgumentException($"periodo da ocorrência nulo! linha {linha.RowNumber()}")],
                    Latitude = latitudeValor,
                    Longitude = longitudeValor,
                    NumeroBoletim = numeroBoletim,
                    TipoOcorrencia = TipoOcorrencia.RouboCelular
                };
                context.OcorrenciasCol.InsertOne(ocorrencia);
                linha = linha.RowBelow();
            }

            Console.WriteLine("executado com sucesso");
        }
    }
}
