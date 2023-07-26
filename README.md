# 태고 곡 랜덤 추천기 
 
## 만든 계기
- 크롤링 개념 공부 좀 할겸 제작 시작함.

- Taiko_Random_Song (태고 곡 랜덤 추천기)
	- Python으로 공식 웹사이트(https://taiko.namco-ch.net/) 크롤링해서 곡 정보 텍스트파일에 저장 (완성)
	- Python으로 짠 크롤링 코드 C#으로 변환 -> 프로그램을 킬때마다 크롤링 구동(새로고침 버튼은 추후에 구현) -> WPF UI(데이터그리드)화면에 실시간으로 적용시키기 (거의 완성)
	- YOUTUBE API KEY 사용해서 검색기능창으로 데이터그리드에 뜨는 실시간 노래 제목 정보 (구현중)
	- 로그인, 회원가입 기능 구현 후 DB와 연결하여 즐겨찾기 기능 및 서열표 체크 구현하기 (미구현)
	
	

크롤링 시작

<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/Taiko_crolling1.png" width="700">

크롤링 중간 단계

<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/Taiko_crolling2.png" width="700">


크롤링 끗

<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/Taiko_crolling3.png" width="700">
<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/Taiko_crolling4.png" width="700">


이건 대충 UI짠거

<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/RandamTaikoApp1.png" width="700">


실시간 정보 크롤링 후 데이터 그리드로 구현( 장르명은 안맞아서 다시 코딩해야함 )

<img src="https://raw.githubusercontent.com/codingnewbie0/RandomTaikoSong/main/images/RandamTaikoApp2.gif" width="700">
