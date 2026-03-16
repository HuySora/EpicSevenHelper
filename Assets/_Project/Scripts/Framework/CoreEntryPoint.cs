using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace SoraTehk.E7Helper {
    public class CoreEntryPoint : MonoBehaviour, IAsyncStartable {
        public async UniTask StartAsync(CancellationToken ct = default) {
        }
    }
}