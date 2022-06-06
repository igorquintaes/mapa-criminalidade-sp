namespace DadosCriminalizacaoSP.AtualizarDados
{
    public class RespostaApiObterCoordenadas
    {
        public List<RepostasCoordenadas> Data { get; set; }

        public RespostaApiObterCoordenadas(List<RepostasCoordenadas> data)
        {
            Data = data;
        }
    }

    public class RepostasCoordenadas
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
