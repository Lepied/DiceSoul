# 🎲 DiceSoul

**DiceSoul**은 주사위 기반의 로그라이크 덱빌딩 게임으로, 주사위 값을 통한 전략적인 전투와 다양한 유물 시스템이 어우러진 게임입니다.

[![Unity](https://img.shields.io/badge/Unity-2022.3+-black.svg?style=flat&logo=unity)](https://unity.com/)

---

## 📖 게임 소개

주사위를 굴려 족보를 완성하고 적을 물리치세요! 매 웨이브마다 새로운 주사위와 유물을 획득하며, 끊임없이 강해지는 적들의 공격을 막아내고 물리치는게 목표입니다.

### ✨ 핵심 특징

- **🎲 전략적 주사위 시스템**: D4, D6, D8, D12, D20 등 다양한 주사위로 덱 구성
- **🃏 족보 조합**: 총합, 원 페어, 투 페어, 스트레이트, 포카드, 풀 하우스 등
- **🔮 60+ 유물**: 각기 다른 효과를 가진 유물로 자신만의 빌드 구축
- **🗺️ 로그라이트 구조** : 강력한 적에게 밀려나더라도 재화를 모아 더욱 성장
- **💰 상점 & 경제 시스템**: 골드를 모아 주사위, 유물, 포션 구매
- **⚔️ 동적 난이도**: 플레이 진행에 따라 적의 체력과 공격력이 증가

---

## 🎮 게임플레이

### 기본 흐름
1. **주사위 굴리기**: 덱의 주사위들을 굴려 결과 확인
2. **주사위 고정**: 원하는 주사위를 Lock하여 보존
3. **재굴림**: 남은 굴림 횟수로 더 좋은 조합 시도
4. **족보 선택**: 완성된 족보를 선택하여 공격 실행
5. **적 처치**: 모든 적을 처치하면 다음 웨이브로 진행

### 족보 예시
- **원 페어** (1+1): 2개의 같은 숫자
- **투 페어** (1+1+2+2): 2쌍의 페어 
- **트리플** (1+1+1): 3개의 같은 숫자
- **스트레이트** (1+2+3+4+5): 연속된 숫자 
- **풀 하우스** (1+1+1+2+2): 트리플 + 원 페어 
- **포카드** (1+1+1+1): 4개의 같은 숫자


---

## 📥 다운로드

### 최신 버전

**[📦 DiceSoul v0.1.0 다운로드 (Windows)](https://lepied.itch.io/dicesoul)**

> ⚠️ **시스템 요구사항**
> - OS: Windows 10/11 (64-bit)
> - DirectX: Version 11
> - 저장 공간: 500 MB

### 빌드 파일
- `DiceSoul_v00.zip` - 압축 해제 후 `DiceSoul.exe` 실행

---

## 🛠️ 기술 스택

- **Engine**: Unity 6.3 LTS
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP)
- **Architecture**: Event-Driven Architecture
- **Data Management**: ScriptableObject + CSV Pipeline
- **Optimization**: Object Pooling, Context Reuse Pattern

### 주요 기능 구현

#### 📊 데이터 기반 설계
- CSV 파일로 유물 관리 → Unity 에디터에서 ScriptableObject 자동 생성


#### ⚡ 성능 최적화
- **GC-Zero 패턴**: Context 객체 재사용으로 웨이브당 ~9KB GC 절감
- **객체 풀링**: VFX, UI 요소 재사용

#### 🎯 이벤트 시스템
- 중앙집중식 GameEvents로 느슨한 결합 구현
- 100+ 유물의 다양한 효과를 확장 가능한 구조로 관리

---

## 🚀 플레이 가이드

### 초보자 팁
1. **주사위 관리**: 초반에는 D6를 많이 모으는 것이 안정적입니다
2. **유물 선택**: "네잎 클로버" (굴림 +1)는 초반 필수 유물
3. **골드 관리**: 상점에서 유물보다 주사위를 먼저 구매하세요
4. **족보 우선순위**: 모든 적에게 균일하게 피해를 누적시키는 족보가 좋습니다.(트리플, 피해량 40이상의 총합 등)
5. **재굴림 타이밍**: 마지막 굴림에서는 확실하게 적을 마무리하는 것을 노리세요



---


## 📂 프로젝트 구조

```
DiceSoul/
├── Assets/
│   ├── Scripts/           # C# 스크립트
│   │   ├── GameManager.cs      # 게임 상태 관리
│   │   ├── StageManager.cs     # 전투 로직
│   │   ├── DiceController.cs   # 주사위 시스템
│   │   ├── RelicEffectHandler.cs # 유물 효과 처리
│   │   └── Events/             # 이벤트 시스템
│   ├── Data/              # CSV 데이터
│   │   └── Relic_Plan.csv      # 유물 데이터
│   ├── Prefabs/           # 프리팹
│   ├── Scenes/            # 씬 파일
│   ├── ScriptableObjects/ # SO 에셋
│   ├── Sprites/           # 2D 그래픽
│   └── VFX/               # 시각 효과
```


## 📧 연락처

- **개발자**: Lepied
- **이메일**: bg3049@gmail.com

---


<div align="center">

Made with ❤️ and 🎲

</div>
