namespace Components
{
    using System;
    using System.Threading.Tasks;
    using Simulator.Utilities;
    using UnityEngine;

    public class RainCollider : MonoBehaviour
    {
        public enum Resolution
        {
            Res32 = 32,
            Res64 = 64,
            Res128 = 128
        }

        public Resolution resolution = Resolution.Res32;

        public SignedDistanceFieldGenerator.SignedDistanceFieldData Data { get; private set; }

        private void Start()
        {
            var cs = RuntimeSettings.Instance.SignedDistanceFieldShader;
            Data = SignedDistanceFieldGenerator.Generate(gameObject, cs, (int) resolution);
        }

        private void OnDestroy()
        {
            Data.texture.Release();
        }
    }
}