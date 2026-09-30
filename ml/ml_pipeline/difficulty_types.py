"""
Unity 쪽 Assets/Script/NpcTip/DifficultyType.cs의 enum과 반드시 같은 순서를 유지해야 함.
이 순서가 ONNX 모델 출력(softmax) 인덱스 <-> 클래스 이름 매핑의 기준이 됨.
"""

DIFFICULTY_TYPES = [
    "EquipmentLack",   # 장비 부족
    "CombatStruggle",  # 전투 미숙
    "Lost",            # 길 잃음
    "ResourceLack",    # 자원 부족
    "Smooth",          # 순조로움
]

DIFFICULTY_TYPE_TO_INDEX = {name: i for i, name in enumerate(DIFFICULTY_TYPES)}
INDEX_TO_DIFFICULTY_TYPE = {i: name for i, name in enumerate(DIFFICULTY_TYPES)}
