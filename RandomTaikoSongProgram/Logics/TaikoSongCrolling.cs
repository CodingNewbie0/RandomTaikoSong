using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using RandomTaikoSongProgram.Logics;
using RandomTaikoSongProgram.Models;

class Program // 프로그램 시작
{
    static async Task Main(string[] args) // 비동기 메인 메서드로 변경
    {
        await TaikoSongCrolling.RunProgram(args); // 비동기로 RunProgram 메서드 실행
    }
}

class TaikoSongCrolling
{
    // DB 연결
    public static async Task RunProgram(string[] args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }
        // MySQL 연결 정보 설정
        string server = "210.119.12.53"; // MySQL 서버 주소
        string database = "taiko"; // 데이터베이스 이름
        string uid = "root"; // 사용자 이름
        string password = "12345"; // 비밀번호
        string connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};";

        // RunCrowling 메서드를 비동기로 실행
        await RunCrowling(connectionString);
    }

    // 크롤링 시작

    static async Task RunCrowling(string connectionString)
    {
        // 크롤링할 링크 목록
        string[] links = {
            "pops",
            "kids",
            "anime",
            "vocaloid",
            "game",
            "variety",
            "classic",
            "namco"
        };

        // Timer 생성 및 설정
        Timer timer = new Timer
        {
            Interval = TimeSpan.FromHours(24).TotalMilliseconds // 24시간 (1일) 간격으로 실행
        };
        timer.Elapsed += async (sender, e) => await OnTimerElapsedAsync(connectionString, links); // 비동기 이벤트 핸들러로 변경
        timer.Start();

        // 프로그램 실행 유지 (24시간마다 크롤링 작업이 실행됩니다)
        Console.WriteLine("크롤링 작업이 매일 00:00에 실행됩니다. 종료하려면 'q'를 입력하세요.");

        // Save song information to DB
        async Task OnTimerElapsedAsync(string myConnString, string[] songLinks)
        {
            foreach (string link in links)
            {
                string url = $"https://taiko.namco-ch.net/taiko/songlist/{link}.php";
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // list to store results
                List<Dictionary<string, object>> taikoInfoList = new List<Dictionary<string, object>>();

                // find the song list table
                var songTable = doc.DocumentNode.SelectSingleNode("//table");

                if (songTable != null)
                {
                    // find row in table
                    var rows = songTable.SelectNodes(".//tr");

                    for (int i = 2; i < rows.Count; i++)
                    {
                        var row = rows[i];

                        // find composer
                        var artistCells = row.SelectNodes(".//p");
                        string artist = "";
                        if (artistCells.Count > 1)
                        {
                            artist = artistCells[0].InnerText.Trim();
                        }
                        else
                        {
                            artist = artistCells[0]?.InnerText.Trim() ?? "";
                        }

                        // find song title
                        var titleCells = row.SelectNodes(".//th");
                        string title = "";
                        if (titleCells != null)
                        {
                            foreach (var cell in titleCells)
                            {
                                var pTag = cell.SelectSingleNode(".//p");
                                pTag?.Remove();
                            }

                            title = titleCells.Count > 1 ? titleCells[0].InnerText.Trim() : titleCells[0]?.InnerText.Trim() ?? "";
                        }

                        // find difficulty
                        var difficultyCells = row.SelectNodes(".//td");
                        var difficultyList = new List<string>();
                        if (difficultyCells != null)
                        {
                            foreach (var cell in difficultyCells)
                            {
                                difficultyList.Add(cell.InnerText.Trim());
                            }
                        }

                        // Sorting detail level and difficulty (except Papama support)
                        if (difficultyList.Count == 6)
                        {
                            string[] difficultyLevels = { "Easy", "Normal", "Hard", "Oni", "Ura" };
                            difficultyList = new List<string>();
                            for (int j = 1; j < 6; j++)
                            {
                                difficultyList.Add($"{difficultyLevels[j - 1]} : {difficultyList[j]}");
                            }
                        }

                        // Add song information to the list
                        var taikoInfo = new Dictionary<string, object>
                {
                    { "Title", title },
                    { "Composer", artist },
                    { "Difficulty", difficultyList }
                };
                        taikoInfoList.Add(taikoInfo);
                    }
                }

                await SaveToTextFile(taikoInfoList);
                await SaveToDatabase(taikoInfoList);
            }
        }

        // Save song information to text file
        async Task SaveToTextFile(List<Dictionary<string, object>> taikoInfoList)
        {
            try
            {
                string fileName = "SongInformation.txt";
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    foreach (var taikoInfo in taikoInfoList)
                    {
                        string title = taikoInfo["Title"].ToString();
                        string composer = taikoInfo["Composer"].ToString();
                        string difficulty = string.Join(", ", (List<string>)taikoInfo["Difficulty"]);

                        await writer.WriteLineAsync($"Title: {title}");
                        await writer.WriteLineAsync($"Composer: {composer}");
                        await writer.WriteLineAsync($"Difficulty: {difficulty}");
                        await writer.WriteLineAsync();
                    }
                }

                await Commons.ShowMessageAsync("Save", "Song information saved to text file.");
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("Error", $"Error saving song information to text file: {ex.Message}");
            }
        }

        // Save song information to database
        async Task SaveToDatabase(List<Dictionary<string, object>> taikoInfoList)
        {
            List<YoutubeSongItem> list = new List<YoutubeSongItem>();

            foreach (var taikoInfo in taikoInfoList)
            {
                string title = taikoInfo["Title"].ToString();
                string composer = taikoInfo["Composer"].ToString();
                string difficulty = string.Join(", ", (List<string>)taikoInfo["Difficulty"]);

                // Create a new YoutubeSongItem object and add it to the list
                var songItem = new YoutubeSongItem
                {
                    Title = title,
                    Composer = composer,
                    Difficulty = difficulty,
                    Genre = "", // 적절한 장르를 여기에 채워주세요
                    Release = DateTime.Now // 가능하다면 릴리스 날짜를 적절하게 수정해주세요
                };

                list.Add(songItem);
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Commons.myConnString))
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    var query = @"INSERT INTO YoutubeSongItem
                            (Title, Composer, Difficulty, Genre, Release)
                            VALUES
                            (@Title, @Composer, @Difficulty, @Genre, @Release)";

                    var insRes = 0;
                    foreach (YoutubeSongItem item in list)
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Title", item.Title);
                        cmd.Parameters.AddWithValue("@Composer", item.Composer);
                        cmd.Parameters.AddWithValue("@Difficulty", item.Difficulty);
                        cmd.Parameters.AddWithValue("@Genre", item.Genre);
                        cmd.Parameters.AddWithValue("@Release", item.Release);

                        insRes += cmd.ExecuteNonQuery();
                    }

                    if (list.Count == insRes)
                    {
                        await Commons.ShowMessageAsync("Save", "Song saved successfully");
                        return;
                    }
                    else
                    {
                        await Commons.ShowMessageAsync("Save", "Error saving song. Contact the administrator.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("Error", $"Error saving song: {ex.Message}");
            }
        }

    }
}
