using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CodeNation.ConsoleApp
{
    class Program
    { 
        private static string token = "MyTokenCodeNation";

        static void Main(string[] args)
        {
            var jsonRetorno = ObterReq();
            var r = JsonConvert.DeserializeObject<RetornoRequisicao>(jsonRetorno);

            Console.WriteLine("Num Casas: " + r.numero_casas.ToString());
            Console.WriteLine("Token: " + r.token);
            Console.WriteLine("Cifrado: " + r.cifrado);

            var decifrado = Decrifrar(r.cifrado, r.numero_casas);
            Console.WriteLine("Decifrado: " + decifrado);

            SHA1Managed sha1 = new SHA1Managed();
            byte[] bytesHash = sha1.ComputeHash(Encoding.UTF8.GetBytes(decifrado));
            string hash = BitConverter.ToString(bytesHash).Replace("-", "").ToLower();
            Console.WriteLine("HASH SHA1: " + hash);

            r.decifrado = decifrado;
            r.resumo_criptografico = hash;

            string resultado = JsonConvert.SerializeObject(r);
            var sw = new StreamWriter(Environment.CurrentDirectory + "/answer.json");
            sw.WriteLine(resultado);
            sw.Close();

            Console.WriteLine("Resultado: " + resultado);

            _ = PostFileAsync();

            Console.ReadKey();
        }

        private static async Task PostFileAsync()
        {
            byte[] paramFileBytes = Converter(new StreamReader(Environment.CurrentDirectory + "/answer.json"));
            HttpContent bytesContent = new ByteArrayContent(paramFileBytes);
            using (var client = new HttpClient())
            { 
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(bytesContent, "answer", "answer");
                    var response = await client.PostAsync($"https://api.codenation.dev/v1/challenge/dev-ps/submit-solution?token={token}", formData);
                    if (response.IsSuccessStatusCode)
                    {
                        var dados = await response.Content.ReadAsStreamAsync();
                        Console.WriteLine("Retorno do POST: " + new StreamReader(dados).ReadToEnd());
                    }
                }
            }
        }
        private static  byte[] Converter(StreamReader reader)
        {
            var bytes = default(byte[]);
            using (var memstream = new MemoryStream())
            {
                reader.BaseStream.CopyTo(memstream);
                bytes = memstream.ToArray();
            }
            return bytes;
        }

        static string ObterReq()
        {
            var request = WebRequest.Create($"https://api.codenation.dev/v1/challenge/dev-ps/generate-data?token={token}");

            var response = request.GetResponse();
            var sr = new StreamReader(response.GetResponseStream());
            var strRetorno = sr.ReadToEnd();

            sr.Close();
            response.Close();

            return strRetorno;
        }
        static string Decrifrar(string cifrado, int numCasas)
        {
            var decifrado = "";
            for (int i = 0; i < cifrado.Length; i++)
            {
                char letra = cifrado.Substring(i, 1).ToCharArray()[0];
                int codigoASCII = (int)letra;
                int codigoFinal = codigoASCII - numCasas;

                if (codigoASCII >= 65 && codigoASCII <= 90)
                {
                    if (codigoFinal < 65)
                    {
                        codigoFinal = 90 - (65 - codigoFinal);
                    }
                    decifrado += Convert.ToChar(codigoFinal);
                }
                else if (codigoASCII >= 97 && codigoASCII <= 122)
                {
                    if (codigoFinal < 97)
                    {
                        codigoFinal = 123 - (97 - codigoFinal);
                    }
                    decifrado += Convert.ToChar(codigoFinal);
                }
                else
                {
                    decifrado += letra;
                }
            }
            return decifrado;
        }
    }
}
