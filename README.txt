# PDF → PNG 변환기

Python 없이 Windows용 EXE를 GitHub Actions에서 자동으로 빌드하는 프로젝트입니다.

## 최종 사용 방법

GitHub Actions가 빌드를 완료하면 `PDF_to_PNG_Windows.zip`을 다운로드합니다.

압축을 풀면:

    PDF_to_PNG.exe

하나가 나옵니다.

이 EXE는 .NET을 별도로 설치하지 않아도 실행하도록 self-contained로 게시됩니다.

## 기능

- PDF 여러 개 선택
- PDF 드래그 앤 드롭
- 150 / 200 / 300 / 400 / 600 DPI
- 페이지별 PNG 저장
- 0001.png, 0002.png 형식
- PDF마다 `[PDF이름]_PNG` 폴더 자동 생성
- 진행률 표시
- 한글 파일명 지원

## GitHub에서 빌드하기

1. GitHub 계정으로 로그인합니다.
2. 새 Repository를 만듭니다.
3. 이 프로젝트의 파일 전체를 업로드합니다.
4. `Actions` 탭으로 이동합니다.
5. `Build Windows EXE` 워크플로를 선택합니다.
6. `Run workflow`를 누릅니다.
7. 빌드가 끝나면 해당 실행의 `Artifacts`에서
   `PDF_to_PNG_Windows`를 다운로드합니다.

PDF 렌더링에는 PDFium 기반 PDFtoImage 5.4.0을 사용합니다.
