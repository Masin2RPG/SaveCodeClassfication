# ??? 보안 가이드 - 데이터베이스 설정

## ?? 중요: 실제 데이터베이스 정보는 절대 GitHub에 업로드하지 마세요!

### 환경변수 설정 방법

#### 1. Windows 환경변수 설정
```cmd
# 시스템 환경변수 설정 (관리자 권한 필요)
setx DB_HOST "실제서버IP" /M
setx DB_PORT "3306" /M  
setx DB_NAME "masinsave" /M
setx DB_USER "사용자명" /M
setx DB_PASSWORD "비밀번호" /M
setx DB_TIMEOUT "30" /M
setx DB_USE_SSL "false" /M
```

#### 2. 배포 시 설정 파일 생성
실제 배포 시에는 `appsettings.json` 파일을 별도로 생성하여 사용하세요:

```json
{
  "DatabaseSettings": {
    "Host": "실제서버IP",
    "Port": 3306,
    "Database": "masinsave", 
    "UserId": "실제사용자명",
    "Password": "실제비밀번호",
    "ConnectionTimeout": 30,
    "UseSSL": false
  }
}
```

#### 3. Git에서 제외할 파일들
다음 파일들은 반드시 `.gitignore`에 포함되어야 합니다:
- `appsettings.json`
- `config.json`
- `database.config`
- `Models/AppSettings.cs` (실제 정보가 하드코딩된 경우)

### 개발자 가이드

#### 로컬 개발 환경
- 기본값은 `localhost`로 설정되어 있습니다
- 로컬 MySQL 서버를 사용하여 개발하세요

#### 프로덕션 환경  
- 환경변수나 설정 파일을 통해 실제 서버 정보를 제공하세요
- 절대 하드코딩하지 마세요!

### 보안 체크리스트
- [ ] 실제 서버 IP 하드코딩 제거
- [ ] 실제 비밀번호 하드코딩 제거  
- [ ] .gitignore에 설정 파일들 추가
- [ ] 환경변수 설정 완료
- [ ] 배포 전 보안 검토 완료