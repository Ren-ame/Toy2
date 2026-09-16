public enum DamageResult
{
    Ignored, // 무적 상태라 맞지 않음
    Parried, // 패링에 성공
    Hit,     // 정상적으로 맞음
    Killed   // 이 타격으로 죽음
}
