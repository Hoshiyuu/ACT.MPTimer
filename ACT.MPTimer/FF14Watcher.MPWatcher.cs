﻿namespace ACT.MPTimer
{
    using System;
    using System.Linq;

    using ACT.MPTimer.Properties;

    /// <summary>
    /// FF14を監視する MPウォッチャー
    /// </summary>
    public partial class FF14Watcher
    {
        /// <summary>
        /// 最後に回復した日時
        /// </summary>
        public DateTime LastRecoveryDateTime { get; private set; }

        /// <summary>
        /// 次に回復するであろう日時
        /// </summary>
        public DateTime NextRecoveryDateTime { get; private set; }

        /// <summary>
        /// 最後にMPが満タンになった日時
        /// </summary>
        public DateTime LastMPFullDateTime { get; private set; }

        /// <summary>
        /// 直前のMP
        /// </summary>
        private int PreviousMP { get; set; }

        /// <summary>
        /// MP回復スパンを監視する
        /// </summary>
        public void WacthMPRecovery()
        {
            var vm = MPTimerWindow.Default.ViewModel;

            var player = FF14PluginHelper.GetCombatantPlayer();
            if (player == null)
            {
                vm.Visible = false;
                return;
            }

            // ジョブ指定？
            if (Settings.Default.TargetJobId != 0)
            {
                vm.Visible = player.Job == Settings.Default.TargetJobId;
                if (!vm.Visible)
                {
                    return;
                }
            }
            else
            {
                vm.Visible = true;
            }

            // 戦闘中のみ稼働させる？
            if (Settings.Default.CountInCombat)
            {
                // MPが満タンになった？
                if (player.CurrentMP > this.PreviousMP &&
                    player.CurrentMP >= player.MaxMP)
                {
                    this.LastMPFullDateTime = DateTime.Now;
                }

                // 現在がMP満タン状態？
                if (player.CurrentMP >= player.MaxMP ||
                    this.PreviousMP < 0)
                {
                    // 前回の満タンからn秒以上経過した？
                    if ((DateTime.Now - this.LastMPFullDateTime).TotalSeconds >=
                        Settings.Default.CountInCombatSpan)
                    {
                        vm.InCombat = false;
                    }
                }
                else
                {
                    vm.InCombat = true;
                }
            }

            // 自然回復による回復量を求める
            var mpRecoveryValueNorml = (int)Math.Floor(player.MaxMP * Constants.MPRecoveryRate.Normal);
            var mpRecoveryValueInCombat = (int)Math.Floor(player.MaxMP * Constants.MPRecoveryRate.InCombat);
            var mpRecoveryValueUI1 = (int)Math.Floor(player.MaxMP * Constants.MPRecoveryRate.UmbralIce1);
            var mpRecoveryValueUI2 = (int)Math.Floor(player.MaxMP * Constants.MPRecoveryRate.UmbralIce2);
            var mpRecoveryValueUI3 = (int)Math.Floor(player.MaxMP * Constants.MPRecoveryRate.UmbralIce3);

            var mpRecoveryValues = new int[]
            {
                mpRecoveryValueNorml,
                mpRecoveryValueNorml + mpRecoveryValueUI1,
                mpRecoveryValueNorml + mpRecoveryValueUI2,
                mpRecoveryValueNorml + mpRecoveryValueUI3,
                mpRecoveryValueInCombat,
                mpRecoveryValueInCombat + mpRecoveryValueUI1,
                mpRecoveryValueInCombat + mpRecoveryValueUI2,
                mpRecoveryValueInCombat + mpRecoveryValueUI3,
            };

            var now = DateTime.Now;

            // MPが回復している？
            if (this.PreviousMP > -1 &&
                player.CurrentMP > this.PreviousMP)
            {
                // 今回の回復量を算出する
                var mpRecoveryValue = player.CurrentMP - this.PreviousMP;

                // 算出した回復量と一致する？
                if (mpRecoveryValues.Any(x => x == mpRecoveryValue))
                {
                    this.LastRecoveryDateTime = now;
                    this.NextRecoveryDateTime = this.LastRecoveryDateTime.AddSeconds(Constants.MPRecoverySpan);
                }
            }

            if (this.NextRecoveryDateTime <= DateTime.MinValue)
            {
                this.NextRecoveryDateTime = now.AddSeconds(Constants.MPRecoverySpan);
            }

            // 回復までの残り時間を算出する
            var remain = (this.NextRecoveryDateTime - now).TotalMilliseconds;

            // 回復までの時間が過ぎている？
            if (remain <= 0.0d)
            {
                this.LastRecoveryDateTime = now.AddMilliseconds(remain);
                this.NextRecoveryDateTime = this.LastRecoveryDateTime.AddSeconds(Constants.MPRecoverySpan);
            }

            if (remain < 0d)
            {
                remain = 0d;
            }

            // ViewModelにセットする
            vm.TimeToRecovery = remain;

            // 現在のMPを保存する
            this.PreviousMP = player.CurrentMP;
        }
    }
}
