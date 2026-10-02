namespace BeforeJericho.UI
{
    /// <summary>UX memo and HUD zone guides from Before Jericho UI.html (SPEC / G / GUIDES). Read only by DevOverlay (Editor / Development builds).</summary>
    public static class DevMemo
    {
        public static readonly System.Collections.Generic.Dictionary<string, (string title, string lead, string[][] zones)> Screens = new()
        {
            ["boot"] = ("첫 실행 설정 (RE Requiem 방식)", "게임을 처음 켤 때 한 번만 표시. 언어·자막·크기·화자 이름을 먼저 정하고, 바로 아래 미리보기에 반영된다. 이 화면은 PlayerPrefs에 저장하고 이후엔 건너뛴다.", new[]{ new[]{"위치", "화면 중앙, 좁은 단일 칼럼"}, new[]{"미리보기", "실제 자막 컴포넌트를 그대로 재사용"}, new[]{"참고", "RE Requiem 첫 실행 · TLOU2 자막 옵션"} }),
            ["menu"] = ("메인 메뉴", "로고 BEFORE JERICHO는 영어 고정. 메뉴 항목만 로컬라이즈. 좌측 하단 정렬로 오른쪽 도로 장면을 살린다. CONTINUE 아래에 마지막 저장 정보.", new[]{ new[]{"로고", "좌상단, 영어 고정 (로컬라이즈 안 함)"}, new[]{"메뉴", "좌측 세로 리스트, 선택 시 앰버 대시"}, new[]{"버튼 폭", "한국어가 더 길어도 깨지지 않게 폭 고정 없음 · 좌정렬"}, new[]{"참고", "RE2/RE4 메인 메뉴 구조"} }),
            ["card"] = ("로케이션 카드", "챕터 시작 시 3–5초. 지명·날짜는 영어 유지, 한국어 모드일 때만 작은 보조 표기. Space로 건너뛰기.", new[]{ new[]{"위치", "좌하단, 영화 자막 위치"}, new[]{"영어 고정", "GRANTS, NEW MEXICO · OCTOBER 29, 2005"}, new[]{"보조 표기", "ko 전용 key (en 값은 빈 문자열 → 숨김)"} }),
            ["explore"] = ("탐색 · 기본 HUD (RDR2 방식)", "평상시 화면에는 거의 아무것도 없다. 목표는 등장 후 4초 뒤 사라지고, 프롬프트는 대상 근처에서만, 무기·체력은 조준·피격 시에만 나타난다. 화면 위 [조준] 버튼이나 우클릭으로 동적 HUD를 확인.", new[]{ new[]{"목표", "좌상단 토스트 → 4초 후 페이드 (상시 표시 삭제)"}, new[]{"색", "선택·목표 = 말린 피 빨강, 화자·정보 = 차가운 스틸, 불 관련만 엠버"}, new[]{"프롬프트", "중앙 하단 63% 지점, [키] + 동사 + 대상"}, new[]{"자막", "하단 중앙, 폭 62%, 보통 크기 = 화면 폭의 1.3%"}, new[]{"무기", "우하단, 조준 해제 3초 후 페이드"}, new[]{"참고", "RDR2 Dynamic HUD · RE4 prompt"} }),
            ["evidence"] = ("증거 조사 (Dead Space 원칙)", "메시지 한 줄 대신 실제 신문을 화면에 띄운다. 종이 자체는 1962년 영어 원문(세계 속 오브젝트), 오른쪽 오버레이만 번역. 닫으면 “저널에 추가됨” 알림.", new[]{ new[]{"원문", "좌측, 세리프 · 로컬라이즈 안 함 (텍스처/월드 텍스트)"}, new[]{"번역", "우측 오버레이, R로 토글"}, new[]{"딘의 메모", "타자기체는 이 계열 화면에만"}, new[]{"참고", "Dead Space diegetic UI · RE2 파일 뷰어"} }),
            ["combat"] = ("전투 · 대사 (RE4 + TLOU2)", "조준 중: 조준점, 우하단 탄약, 좌하단 상태. 화면 밖 소리는 [캡션] + 방향 화살표. Space로 다음 대사.", new[]{ new[]{"자막 순서", "캡션 줄 → 화자 이름 + 대사 줄"}, new[]{"방향", "◀ ▶ 화살표 (설정에서 끄기 가능)"}, new[]{"사냥 수칙", "좌상단 카드, 챕터 첫 조우 1회. ‘힌트’ 대신 세계관 용어 (RE4는 ‘팁’·‘튜토리얼’)"}, new[]{"피격", "화면 가장자리 붉은 비네트 + 상태 바 표시"}, new[]{"참고", "RE4 전투 HUD · TLOU2 방향 자막"} }),
            ["ritual"] = ("의식 · 길게 눌러 태우기", "E를 누르고 있는 동안 링이 찬다 (6초). 피격 시 1초 감소 — 현재 게임 규칙 그대로. 라이터 상태는 프롬프트 보조줄에 표시.", new[]{ new[]{"링", "프롬프트 키를 감싸는 원형 진행"}, new[]{"보조줄", "라이터 켜짐 / 필요"}, new[]{"완료", "캡션 [불길이 치솟는다] + 대사"} }),
            ["dead"] = ("게임 오버", "사망 원인에 맞춘 힌트 한 문단 + 체크포인트 이름. 현재 HuntHUD의 챕터별 문구를 System 테이블로 옮긴다.", new[]{ new[]{"힌트", "System/dead.hint.<chapter>"}, new[]{"버튼", "계속하기(마지막 체크포인트) / 메인 메뉴로 돌아가기 — RE4 게임 오버와 같은 용어"} }),
            ["complete"] = ("챕터 완료", "제목은 세계 속 단어(JERICHO, CA.) 그대로. 요약 통계와 다음 챕터 안내.", new[]{ new[]{"제목", "영어 고정 (en/ko 동일 값)"}, new[]{"통계", "단서 · 시간 · 사망"}, new[]{"다음", "다음 챕터 지명"} }),
            ["credits"] = ("크레딧", "기존 CH06 종료 텍스트를 분리한 롤. 역할명만 로컬라이즈.", new[]{ new[]{"롤", "중앙 정렬 자동 스크롤"}, new[]{"감사", "System/credits.thanks"} }),
            ["pause"] = ("일시 정지", "좌측 메뉴, 우측에 현재 목표·체크포인트·플레이 시간. 조작 설명 문단은 삭제하고 설정 › 조작으로 이동.", new[]{ new[]{"메뉴", "계속하기 / 저널 / 지도 / 옵션 / 체크포인트 재시작 / 메인 메뉴로 돌아가기"}, new[]{"우측", "목표를 다시 확인하는 곳 (HUD에 상시 표시하지 않으므로)"}, new[]{"참고", "RE2 pause · RDR2 요약"} }),
            ["journal"] = ("저널 (RE2 정보 구조 + 헌터 저널)", "사건 / 단서 / 인물 / 자료 / 소지품 / 지도 탭. 발견하지 않은 단서는 ‘미확인’으로만. 딘의 메모는 타자기체 종이.", new[]{ new[]{"탭", "Q / E 전환 · Tab 닫기"}, new[]{"사건", "현재 목표 · 위치 · 약점 · 존의 흔적"}, new[]{"단서", "n / 5 진행도 + 미확인 슬롯"}, new[]{"메모", "결론은 필요한 단서가 모두 모였을 때만"} }),
            ["map"] = ("지도 (RE2 방식)", "상시 미니맵 없음. M으로 연다. 방 상태를 채움/점선으로 구분, 색은 목표(앰버)와 위험(빨강)에만.", new[]{ new[]{"현재 위치", "흰 삼각형"}, new[]{"목표", "앰버 별"}, new[]{"미확인 방", "점선"}, new[]{"벽선", "5–9호실 위 앰버 점선 = 가설 표시"}, new[]{"지명", "SUNSET STAR MOTOR COURT 영어 고정"} }),
            ["settings"] = ("옵션 (RE Requiem + COD + TLOU2)", "7개 탭. 용어는 RE4 한국어판 기준(옵션·HUD 표시·접근성), 값은 동사(켜기) 대신 상태(켜짐/꺼짐). 언어·자막·HUD 설정은 이 프로토타입 전체에 실제로 반영된다. 오른쪽 설명 + 실시간 미리보기.", new[]{ new[]{"좌", "카테고리 탭"}, new[]{"중", "항목 ◀ 값 ▶ / 슬라이더"}, new[]{"우", "설명 + 자막 미리보기 / 밝기 보정"}, new[]{"조작", "클릭 후 키 입력으로 리매핑"} }),
        };
        public static readonly System.Collections.Generic.Dictionary<string, (float x, float y, float w, float h, string label)> Zones = new()
        {
            ["toast"] = (3f, 4f, 34f, 14f, "목표 토스트 · 튜토리얼 (일시 표시)"),
            ["save"] = (82f, 4f, 15f, 7f, "저장 아이콘"),
            ["notify"] = (70f, 38f, 27f, 12f, "아이템·저널 알림"),
            ["prompt"] = (35f, 60f, 30f, 8f, "상호작용 프롬프트 (근접 시만)"),
            ["subs"] = (15f, 74f, 70f, 20f, "자막 Safe Area (캡션 → 화자 → 대사)"),
            ["wpn"] = (77f, 72f, 20f, 24f, "무기·탄약 (조준/재장전 시)"),
            ["hp"] = (3f, 86f, 18f, 10f, "상태 (피격 시)"),
            ["xh"] = (46f, 45f, 8f, 10f, "조준점"),
        };
        public static readonly System.Collections.Generic.Dictionary<string, string[]> Guides = new()
        {
            ["explore"] = new[]{ "toast", "save", "notify", "prompt", "subs", "wpn", "hp" },
            ["combat"] = new[]{ "toast", "xh", "subs", "wpn", "hp" },
            ["ritual"] = new[]{ "prompt", "subs", "hp" },
        };
    }
}
