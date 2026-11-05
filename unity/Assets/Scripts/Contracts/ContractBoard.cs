using UnityEngine;
using WhoBrokeIt.Interaction;

namespace WhoBrokeIt.Contracts
{
    /// <summary>One of the three contract boards in the plaza. Its prompt shows the contract currently offered in its slot; [E] starts it.</summary>
    public class ContractBoard : Interactable
    {
        [Range(0, 2)] public int slot;

        public override string Prompt
        {
            get
            {
                var dir = ContractDirector.I;
                if (dir == null || !dir.IsSpawned) return "Sözleşme panosu";
                if (dir.state.Value == ContractDirector.Active) return "Bir sözleşme sürüyor: " + dir.Def(dir.activeId.Value).title;
                var d = dir.Def(ContractDirector.Offer(dir.offers.Value, slot));
                return "Başlat: " + d.title + "  (" + d.goal + " adet → " + d.zoneName + ")";
            }
        }

        protected override void ServerInteract(ulong clientId)
        {
            if (ContractDirector.I != null) ContractDirector.I.ServerStart(slot);
        }
    }
}
