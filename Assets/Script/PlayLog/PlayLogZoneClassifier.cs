using UnityEngine;

/// <summary>
/// SeedMapModel의 스폰 지점 기준 거리 링(Safe/Dirt/Stone·Copper/Silver·Gold) 공식을 재사용해
/// 현재 위치를 구역 라벨로 변환하고, 같은 구역에 머문 시간을 추적.
/// </summary>
public class PlayLogZoneClassifier
{
    private readonly SeedMapModel seedMapModel;

    private string currentZone = "Unknown";
    private float zoneEnteredAtSec;

    public PlayLogZoneClassifier(SeedMapModel seedMapModel)
    {
        this.seedMapModel = seedMapModel;
    }

    public string CurrentZone => currentZone;

    public void Update(Vector3 playerPosition, float nowSec)
    {
        string zone = ClassifyZone(playerPosition);

        if (zone != currentZone)
        {
            currentZone = zone;
            zoneEnteredAtSec = nowSec;
        }
    }

    public float GetDwellTimeSec(float nowSec)
    {
        return Mathf.Max(0f, nowSec - zoneEnteredAtSec);
    }

    private string ClassifyZone(Vector3 playerPosition)
    {
        if (seedMapModel == null)
        {
            return "Unknown";
        }

        float distance = seedMapModel.GetSquareDistanceInTiles(playerPosition);

        if (seedMapModel.IsSafeArea(distance))
        {
            return "Safe";
        }

        if (distance <= seedMapModel.DirtRange)
        {
            return "Dirt";
        }

        if (distance <= seedMapModel.StoneCopperRange)
        {
            return "Stone_Copper";
        }

        if (distance <= seedMapModel.SilverGoldRange)
        {
            return "Silver_Gold";
        }

        return "Gold";
    }
}
