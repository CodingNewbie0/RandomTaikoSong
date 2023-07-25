//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.Linq.Expressions;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using System.Timers;
//using HtmlAgilityPack;
//using MySql.Data.MySqlClient;
//using RandomTaikoSongProgram.Logics;
//using RandomTaikoSongProgram.Models;

//class Program // 프로그램 시작
//{
//    /*
//    static void Main(string[] args) // 비동기 메인 메서드로 변경
//    {
//        //TaikoSongCrolling.RunProgram(args); // 비동기로 RunProgram 메서드 실행
//    }
//    */
//}

//class TaikoSongCrolling
//{
//    public async Task RunProgram(string[] args)
//    {
//        if (args is null)
//        {
//            throw new ArgumentNullException(nameof(args));
//        }
//        // MySQL 연결 정보 설정
//        string server = "210.119.12.53"; // MySQL 서버 주소
//        string database = "taiko"; // 데이터베이스 이름
//        string uid = "root"; // 사용자 이름
//        string password = "12345"; // 비밀번호
//        string connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};";

//        // RunCrowling 메서드를 비동기로 실행
//        //RunCrowling(/*connectionString*/);
//    }

//    // 크롤링 시작



//    public void RunCrowling(string connectionString)
//    {
//        // 크롤링할 링크 목록
//        string[] links = {
//            "pops",
//            "kids",
//            "anime",
//            "vocaloid",
//            "game",
//            "variety",
//            "classic",
//            "namco"
//        };

//        SongCrawlingAsync();

//        // 노래 정보를 크롤링
//        void SongCrawlingAsync()
//        {
//            // 리스트에 배열 저장
//            List<Dictionary<string, object>> taikoInfoList = new List<Dictionary<string, object>>();

//            foreach (string link in links)
//            {
//                // 미리 선언한 내용을 링크와 결합
//                string url = $"https://taiko.namco-ch.net/taiko/songlist/{link}.php";

//                // URL에서 HTML 코드 가져오기
//                WebClient webClient = new WebClient
//                {
//                    Encoding = Encoding.GetEncoding("utf-8")
//                };
//                string htmlCode = webClient.DownloadString(url);

//                // 일본어 문자열을 utf-8로 인코딩(묵음<ㅡ>은 ?로 표시되는 오류있음)
//                byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(htmlCode);
//                string utf8String = Encoding.UTF8.GetString(bytes);

//                // HtmlDocument 객체 생성 및 UTF-8로 인코딩된 HTML 코드 로드
//                HtmlDocument doc = new HtmlDocument();
//                doc.LoadHtml(utf8String);

//                // 노래 리스트 테이블을 찾음
//                HtmlNodeCollection tableNodes = doc.DocumentNode.SelectNodes("//table");

//                if (tableNodes != null)
//                {
//                    // 노래 제목
//                    foreach (HtmlNode rowNode in tableNodes)
//                    {
//                        HtmlNodeCollection titleCells = rowNode.SelectNodes(".//th");
//                        string title = "";
//                        if (titleCells != null)
//                        {
//                            foreach (HtmlNode cellNode in titleCells)
//                            {
//                                var pTag = cellNode.SelectSingleNode(".//p");
//                                pTag?.Remove();
//                                Console.Write(cellNode.InnerText + " ");
//                            }
//                            title = titleCells.Count > 1 ? titleCells[0].InnerText.Trim() : titleCells[0]?.InnerText.Trim() ?? "";
//                            Console.WriteLine();
//                        }
//                    }

//                    // 작곡가명
//                    foreach (HtmlNode rowNode in tableNodes)
//                    {
//                        HtmlNodeCollection artistCells = rowNode.SelectNodes(".//p");
//                        string artist = "";
//                        if (artistCells.Count > 1)
//                        {
//                            artist = artistCells[0].InnerText.Trim();
//                            foreach (HtmlNode cellNode in artistCells)
//                            {
//                                Console.Write(cellNode.InnerText + " ");
//                            }
//                            Console.WriteLine();
//                        }
//                        else
//                        {
//                            artist = artistCells[0]?.InnerText.Trim() ?? "";
//                        }
//                    }

//                    // 난이도 찾기
//                    foreach (HtmlNode rowNode in tableNodes)
//                    {
//                        HtmlNodeCollection difficultyCells = rowNode.SelectNodes(".//td");
//                        var difficultyList = new List<string>();
//                        if (difficultyCells != null)
//                        {
//                            foreach (HtmlNode cellNode in difficultyCells)
//                            {
//                                difficultyList.Add(cellNode.InnerText.Trim());
//                                Console.Write(cellNode.InnerText + " ");
//                            }
//                            Console.WriteLine();
//                        }
//                        // 레벨별 난이도
//                        if (difficultyList.Count == 6)
//                        {
//                            string[] difficultyLevels = { "쉬움", "보통", "어려움", "오니", "우라" };
//                            difficultyList = new List<string>();
//                            for (int j = 1; j < 6; j++)
//                            {
//                                difficultyList.Add($"{difficultyLevels[j - 1]} : {difficultyList[j]}");
//                            }
//                        }
//                    }

//                    // <img> 태그들 선택
//                    HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");

//                    // 장르 찾기
//                    if (imgNodes != null)
//                    {
//                        int n = 0;
//                        foreach (HtmlNode imgNode in imgNodes)
//                        {
//                            // <img> 태그의 alt 속성 확인
//                            string altContent = imgNode.GetAttributeValue("alt", "");
//                            Console.WriteLine(altContent);
//                            n++;
//                            if (n >= 10) break;
//                        }
//                    }


//                    // 노래정보를 리스트에 추가
//                    var taikoInfo = new Dictionary<string, object>
//                        {
//                            { "Title", title },
//                            { "Composer", artist },
//                            { "Difficulty", difficultyList },
//                            { "Genre", genre }
//                        };
//                    taikoInfoList.Add(taikoInfo);
//                    YourDataGrid.ItemsSource = taikoInfoList;
//                }
//            }
//        }
//    }
//}
