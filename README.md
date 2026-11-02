# OutPost

> 자원을 모으고, 보관하고, 제작하며 살아남는 Unity 서바이벌·크래프팅 게임 (개발 중)

![Unity](https://img.shields.io/badge/Unity-6000.3.10f1-black)
![C#](https://img.shields.io/badge/C%23-239120)
![Status](https://img.shields.io/badge/status-in%20development-orange)

---

## 게임 소개

플레이어는 필드에서 자원을 획득하고, 인벤토리와 핫바에 보관하고, 제작대에서 필요한 아이템을 만들어 생존합니다. 아이템 데이터와 인벤토리 상태가 제작 UI에 실시간으로 반영되어, 플레이어가 재료가 얼마나 있는지와 지금 무엇을 만들 수 있는지를 바로 판단할 수 있게 하는 것을 목표로 합니다.

## 개발 정보

| 항목 | 내용 |
|---|---|
| 기간 | 2026.03 ~ 개발 중 |
| 팀 | 3인 |
| 엔진 | Unity 6 (6000.3.10f1) |
| 언어 | C# |
| 그 외 | Blender (3D 에셋), JSON 데이터 구조 |

## 담당 파트 — 이건호

게임 기획, 인벤토리·핫바·제작 시스템과 UI, 그리고 Blender 3D 에셋 제작을 맡고 있습니다.

### 1. MVP 구조로 나눈 인벤토리·제작

아이템 데이터와 게임 규칙을 화면 표시·입력 처리와 분리했습니다.

| 역할 | 클래스 | 하는 일 |
|---|---|---|
| Model | `InventoryModel`, `HotbarModel`, `CraftingModel` | 보유 아이템·수량, 중첩 규칙, 제작 조건 |
| View | `InventorySlotView`, `HotbarView`, `CraftingView`, `CraftingTooltipView` | 슬롯 아이콘·수량 표시, 입력 전달 |
| Presenter | `InventoryPresenter`, `HotbarPresenter`, `CraftingPresenter` | 입력을 Model에 전달하고, 바뀐 데이터로 View 갱신 |

### 2. 인벤토리와 핫바

- 칸 수를 코드가 아닌 인스펙터 값(`maxSlots`, 현재 40칸)으로 정하는 인벤토리, 아이템별 최대 수량(`maxStack`)까지 같은 아이템 중첩
- Ctrl + 클릭으로 스택 나누기
- 인벤토리를 닫아도 핫바는 항상 표시
- NPC 대화나 상점이 열리면(`UIState`) 핫바를 숨기고 입력을 막아 기능 간 입력 충돌 방지

### 3. 검사를 모두 마친 뒤에만 실행하는 제작

```csharp
// Assets/Script/crafting/Model/CraftingModel.cs (요약)
public CraftingResult Craft(CraftingRecipe recipe)
{
    // 1) 재료가 충분한가?          → 아니면 NotEnoughIngredients
    // 2) 결과물을 받을 공간이 있는가? → 아니면 InventoryFull
    // 3) 두 검사를 모두 통과한 뒤에야 재료 차감 (인벤토리 우선, 부족분은 핫바)
    // 4) 결과 지급 (핫바 먼저, 남으면 인벤토리)
    return CraftingResult.Success;
}
```

재료가 부족하거나 결과물을 넣을 공간이 없을 때 재료만 사라지는 일이 없도록, 모든 검사를 끝낸 뒤에만 인벤토리를 바꿉니다.

### 4. 제작 툴팁

제작법에 마우스를 올리면 결과 아이템과 필요한 재료가 나옵니다. 재료 줄(`CraftingIngredientRowView`)은 제작법 데이터로 동적으로 만들고, **보유량 / 필요량**을 비교해 부족한 재료는 붉은색으로 표시합니다.

### 5. 해결한 문제 — 제작 후 화면이 바뀌지 않던 버그

제작을 끝내도 보유 수량과 제작 가능 표시가 바로 바뀌지 않았습니다.

- **원인 구분:** 데이터가 안 바뀐 것인지, 데이터는 바뀌었는데 화면이 이전 값을 보여 주는 것인지부터 나눠 확인했습니다. 재료 차감과 지급은 정상이었고, UI가 갱신 전 값을 들고 있었습니다.
- **해결:** `CraftingPresenter`가 `InventoryModel.OnInventoryChanged`와 `HotbarModel.OnHotbarChanged`를 구독해, 데이터가 바뀔 때마다 제작 가능 상태를 다시 계산하도록 연결했습니다.
- **배운 점:** 데이터가 바뀌는 시점과 화면이 그 데이터를 읽는 시점을 함께 설계해야 합니다.

### 6. 확장을 고려한 데이터

아이템과 제작법을 UI 코드에 직접 넣지 않고 별도 데이터(`ItemData`, `CraftingRecipe`)로 관리해, 이후 JSON 기반 데이터로 옮길 수 있게 설계했습니다.

## 팀원 담당 파트

| 이름 | 담당 |
|---|---|
| 이건호 ([@Lee-GunHo](https://github.com/Lee-GunHo)) | 게임 기획 · 인벤토리 · 핫바 · 제작 시스템과 UI · Blender 3D 에셋 |
| [@kyungmin2322](https://github.com/kyungmin2322) | NPC·상점(몬스터 재료 물물교환) · 상자 · 맵 청크 스트리밍·시드 맵 · NPC 난관 진단 팁과 플레이 로그 |
| [@minsang1119](https://github.com/minsang1119) | 몬스터 AI·웨이브 · 보스 · 플레이어 상태·스탯 · 넥서스 파괴 |

## 폴더 구조 (담당 부분)

```
Assets/Script/
├── Inventory/
│   ├── Model/       InventoryModel, HotbarModel, EquipmentModel
│   ├── Presenter/   InventoryPresenter, HotbarPresenter
│   └── View/        슬롯, 핫바, 아이템 툴팁 View
├── crafting/
│   ├── Model/       CraftingModel, CraftingTableModel
│   ├── Presenter/   CraftingPresenter
│   ├── View/        제작창, 레시피 슬롯, 재료 줄, 툴팁
│   └── Data/        CraftingRecipe (ScriptableObject)
└── Item/            ItemData, ItemStack
```

## 실행 방법

1. Unity Hub에서 **Unity 6000.3.10f1**로 프로젝트를 엽니다
2. `Assets/Scenes/MainScene.unity`를 열고 Play 합니다 (메인 → `GameScene`)

## 앞으로 할 일

- 제작 결과 아이템을 마우스로 끌어 옮기는 드래그 상태
- 제작대 종류별 제작법 분리
- JSON 데이터 검증과 편집 편의성 개선
- 저장·불러오기와 인벤토리 상태 연동
