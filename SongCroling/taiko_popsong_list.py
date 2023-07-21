from urllib.request import urlopen
from bs4 import BeautifulSoup

# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/pops.php") # 제이팝
res = urlopen("https://taiko.namco-ch.net/taiko/songlist/kids.php") # 어린이
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/anime.php") # 애니메이션
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/vocaloid.php") # 보컬로이드
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/game.php") # 게임뮤직
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/variety.php") # 버라이어티
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/classic.php") # 클래식
# res = urlopen("https://taiko.namco-ch.net/taiko/songlist/namco.php") # 오리지널
soup = BeautifulSoup(res, "html.parser")

# 결과를 저장할 리스트 생성
taiko_info_list = []

# 노래 목록 테이블 찾기
song_table = soup.find("table")

if song_table is not None:
    # 테이블 안의 각 행 찾기
    rows = song_table.find_all("tr")

    for row in rows[2:]:
        # 작곡가 찾기
        artist_cells = row.find_all("p")
        if len(artist_cells) > 1:
            artist = artist_cells[0].get_text(strip=True)  # 작곡가
        else:
            artist = artist_cells[0].get_text(strip=True) if artist_cells else ""

        # 노래 제목 찾기
        title_cells = row.find_all("th")
        if title_cells:
            p_tags = [cell.find("p") for cell in title_cells]
            for p_tag in p_tags:
                if p_tag:
                    p_tag.extract()
        if len(title_cells) > 1:
            title = title_cells[0].get_text(strip=True)  # 노래 제목
        else:
            title = title_cells[0].get_text(strip=True) if title_cells else ""

        # 난이도 찾기
        difficulty_cells = row.find_all("td")
        if len(difficulty_cells) > 0:
            difficulty_list = [cell.get_text(strip=True) for cell in difficulty_cells]
        else:
            difficulty_list = []

        # Papamama Support 제외하고 난이도 수준 상세화
        if len(difficulty_list) == 6:
            difficulty_levels = ["간단", "보통", "어려움", "오니", "우라"]
            difficulty_list = [f"{level} : {difficulty}" for level, difficulty in zip(difficulty_levels, difficulty_list[1:])]

        # 노래 정보를 리스트에 추가
        taiko_info = {
            "노래 제목": title,
            "작곡가": artist,
            "난이도": difficulty_list
        }
        taiko_info_list.append(taiko_info)

# 노래 정보를 파일에 저장하기 위해 파일 생성
with open("taiko_song_info.txt", "w", encoding="utf-8") as file:
    for taiko_info in taiko_info_list:
        file.write(f"-------------------------------------------------------")
        file.write(f"\n노래 제목 : {taiko_info['노래 제목']}\n")
        file.write(f"작곡가 : {taiko_info['작곡가']}\n")
        if len(taiko_info["난이도"]) > 0:
            file.write("\n난이도\n")
            for difficulty in taiko_info["난이도"]:
                file.write(f"{difficulty}\n")
        else:
            file.write("난이도: None\n")

print("노래 정보가 taiko_song_info.txt 파일에 저장되었습니다.")
