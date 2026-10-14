# Monster AI recorded observations

Input sessions: 1; resolved action records: 328; battle records: 9.

These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.
Missing modes require additional play. Policy versions are kept separate. No values are invented.

Source sessions:

- C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_221126_27d75bac5fd446cc813ca11be625b26b

## Completed battles

| mode | cooperation | squadPolicyVersion | completedBattles | meanDirectDamageToPlayer | meanBattleSeconds | meanOverlapSeconds | meanSurroundSeconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | 2 | 1 | 34 | 113.375 | 0.000 | 0.000 |
| Frozen | False | 2 | 2 | 15 | 45.628 | 0.000 | 0.000 |
| Training | False | 2 | 6 | 32.667 | 93.384 | 0.000 | 0.000 |

## Attack observations (rates are 0..1)

| mode | cooperation | learningSource | squadPolicyVersion | action | selected | releasedResolved | damageHitRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | RegisteredTest | 2 | Melee | 1 | 1 | 1.000 |
| Baseline | False | RegisteredTest | 2 | Projectile | 20 | 18 | 0.000 |
| Baseline | False | RegisteredTest | 2 | Circle | 15 | 11 | 0.182 |
| Baseline | False | RegisteredTest | 2 | Charge | 9 | 9 | 0.889 |
| Frozen | False | RegisteredTest | 2 | Melee | 0 | 0 | N/A |
| Frozen | False | RegisteredTest | 2 | Projectile | 16 | 16 | 0.000 |
| Frozen | False | RegisteredTest | 2 | Circle | 12 | 12 | 0.000 |
| Frozen | False | RegisteredTest | 2 | Charge | 12 | 12 | 0.833 |
| Training | False | RegisteredTest | 2 | Melee | 9 | 9 | 1.000 |
| Training | False | RegisteredTest | 2 | Projectile | 102 | 95 | 0.021 |
| Training | False | RegisteredTest | 2 | Circle | 70 | 62 | 0.048 |
| Training | False | RegisteredTest | 2 | Charge | 62 | 60 | 0.917 |

## Formation decisions

No recorded samples for this table yet.
