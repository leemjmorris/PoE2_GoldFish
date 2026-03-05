using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace PoE2Overlay.Features.PassiveTree.Services
{
    public static class PobDecoder
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// pobb.in URL 또는 PoB2 직접 빌드 코드를 입력받아 할당된 패시브 노드 ID 목록을 반환합니다.
        /// </summary>
        public static async Task<List<int>> DecodeAsync(string input)
        {
            input = input.Trim();

            var match = Regex.Match(input, @"pobb\.in/([A-Za-z0-9_\-]+)");
            if (match.Success)
            {
                string shortCode = match.Groups[1].Value;
                string code = await FetchPobCodeAsync(shortCode);
                return DecodeCode(code);
            }

            return DecodeCode(input);
        }

        private static async Task<string> FetchPobCodeAsync(string shortCode)
        {
            // pobb.in 은 /raw 경로로 base64url 코드를 직접 반환합니다
            try
            {
                string raw = await _http.GetStringAsync($"https://pobb.in/{shortCode}/raw");
                return raw.Trim();
            }
            catch
            {
                throw new InvalidOperationException(
                    "pobb.in에서 빌드 데이터를 가져올 수 없습니다.\n" +
                    "PoB2에서 '빌드 내보내기 → 빌드 코드 복사'를 사용하세요.");
            }
        }

        private static List<int> DecodeCode(string code)
        {
            // base64url → base64 표준 인코딩
            string b64 = code.Replace('-', '+').Replace('_', '/');
            int pad = b64.Length % 4;
            if (pad != 0) b64 += new string('=', 4 - pad);

            byte[] compressed;
            try
            {
                compressed = Convert.FromBase64String(b64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("유효하지 않은 PoB 빌드 코드입니다.", ex);
            }

            // zlib 스트림은 앞 2바이트가 헤더 — DeflateStream은 헤더 없이 처리
            byte[] xmlBytes;
            try
            {
                using var ms = new MemoryStream(compressed, 2, compressed.Length - 2);
                using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
                using var output = new MemoryStream();
                deflate.CopyTo(output);
                xmlBytes = output.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("빌드 코드 압축 해제에 실패했습니다.", ex);
            }

            string xml = Encoding.UTF8.GetString(xmlBytes);
            return ParseNodeIds(xml);
        }

        private static List<int> ParseNodeIds(string xml)
        {
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("빌드 XML 파싱에 실패했습니다.", ex);
            }

            // <Tree><Spec nodes="1,2,3,..."/></Tree>
            var spec = doc.SelectSingleNode("//Spec[@nodes]");
            if (spec?.Attributes?["nodes"]?.Value is not string nodeStr || string.IsNullOrEmpty(nodeStr))
                return new List<int>();

            var result = new List<int>();
            foreach (string part in nodeStr.Split(','))
            {
                if (int.TryParse(part.Trim(), out int id))
                    result.Add(id);
            }
            return result;
        }
    }
}
