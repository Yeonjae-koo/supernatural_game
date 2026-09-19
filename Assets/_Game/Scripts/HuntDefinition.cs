using UnityEngine;
namespace BeforeJericho
{
    [CreateAssetMenu(menuName = "Before Jericho/Hunt Definition")]
    public sealed class HuntDefinition : ScriptableObject
    {
        public float walkSpeed = 3.2f, sprintSpeed = 5.3f, aimSpeed = 1.8f;
        public float ghostSpeed = 2.1f, attackInterval = 2.5f, ironDisperseSeconds = 5f;
        public int ghostDamage = 22, magazineSize = 7;
        public float ignitionSeconds = 8f;
    }
}
