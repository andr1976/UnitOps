"""Evidence for parameter selection: calibrate both mixing models on every validation set.

For each set (validation_sets.SETS) and mixing model (CAM, CPM):
  0. predict with the Screening set (eta_m/eta_s/eta_mix/eta_d = 0.90/0.85/0.90/0.80, Cd 1);
  1. fit Cd_m on the measured motive flow (rating sets only);
  2. fit (eta_s, eta_mix, eta_d) with eta_m = 0.90 fixed, as in calibrate_r744.py.
Transfer: for the single-phase vapour sets (Huang R141b, Falat R134a, Shestopalov
R245fa) a pooled fit, and leave-one-set-out predictions (fit on two sets, predict
the third), so the guide can quote the error of a set used on an ejector it was
not fitted to.

    python calibrate_sets.py            # print everything
gen_data.py (section 'mixing') writes the CSVs used by the technical reference.
"""
import numpy as np
from scipy.optimize import least_squares

import validation_sets as vs

SCREENING = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80, Cd_m=1.0, phi_p=1.0)
NAMES = ("eta_s", "eta_mix")
NAMES_CPM_VAPOUR = ("phi_p", "eta_mix")    # eta_s = 0.85 fixed; phi_p carries the entrainment
ETA_D_FIXED = 0.85    # lift depends on eta_mix and eta_d jointly: with both free the fits trade
                      # one against the other (eta_d ran to its 0.3 bound); fixed at the gas-ejector
                      # value, inside the 0.75-0.9 range assumed in the literature (Liu 2014)


def names_for(name, mixing):
    """Fitted parameters (eta_m 0.90 and eta_d 0.85 fixed): CAM (eta_s, eta_mix); CPM on
    single-phase vapour sets (phi_p, eta_mix) with eta_s 0.85 (with phi_p = 1 the CPM fits drive
    eta_s to its bound of 1: the core-area factor, not the suction efficiency, sets the
    entrainment); CPM on two-phase sets (eta_s, eta_mix) with phi_p = 1 (phi_p fits to 1 there)."""
    return NAMES_CPM_VAPOUR if (mixing == "CPM" and name in VAPOUR) else NAMES
CLASS = dict(Giacomelli2019="two-phase CO2 (transcritical / liquid motive)",
             Lawrence2012="two-phase HFC/HFO (liquid motive, low reduced pressure)",
             Huang1999="single-phase vapour", Falat2018="single-phase vapour",
             Shestopalov2015="single-phase vapour")
VAPOUR = ("Huang1999", "Falat2018", "Shestopalov2015")


def _resid(cases, preds):
    """Dimensionless residuals of one set (see validation_sets.fit)."""
    if cases[0]["kind"] == "rating":
        scale = np.mean([c["m_s"] for c in cases])
        return np.array([np.nan_to_num((q["m_s"] - c["m_s"]) / scale, nan=2.0)
                         for c, q in zip(cases, preds)])
    er_scale = np.mean([c["ER"] for c in cases])
    lift_scale = np.mean([c["P_b"] - c["s0"][1] for c in cases])
    return np.r_[[np.nan_to_num((q["ER"] - c["ER"]) / er_scale, nan=2.0) for c, q in zip(cases, preds)],
                 [np.nan_to_num((q["P_b"] - c["P_b"]) / lift_scale, nan=2.0) for c, q in zip(cases, preds)]]


def fit_multi(sets, params_by_set, names=NAMES):
    """Common (eta_s, eta_mix, eta_d) over several sets; each set keeps its own Cd_m.
    Residuals of each set are normalised by its size so every set weighs equally."""
    def resid(x):
        out = []
        for name, cases in sets.items():
            p = dict(params_by_set[name], **dict(zip(names, x)))
            out.append(_resid(cases, vs.evaluate(cases, p)) / np.sqrt(len(cases)))
        return np.concatenate(out)

    p0 = next(iter(params_by_set.values()))
    r = least_squares(resid, [p0[n] for n in names], bounds=([0.3] * len(names), [1.0] * len(names)),
                      diff_step=0.01, xtol=1e-4, ftol=1e-4)
    dof = max(len(r.fun) - len(names), 1)
    se = np.sqrt(np.diag(np.linalg.pinv(r.jac.T @ r.jac) * (2 * r.cost / dof)))
    return dict(zip(names, r.x)), dict(zip(names, se))


def metrics(cases, preds):
    """Headline error numbers of one set [%]."""
    e = vs.errors(cases, preds)
    nfail = sum(p["regime"] == "fail" for p in preds)
    if "m_s" in e:
        return dict(n=len(cases), m_m_mae=np.nanmean(np.abs(e["m_m"])),
                    m_s_mae=np.nanmean(np.abs(e["m_s_scaled"])), m_s_bias=np.nanmean(e["m_s_scaled"]),
                    er_mae=np.nan, lift_mae=np.nan, lift_bias=np.nan, fails=nfail)
    return dict(n=len(cases), m_m_mae=np.nan, m_s_mae=np.nan, m_s_bias=np.nan,
                er_mae=np.nanmean(np.abs(e["ER"])), er_bias=np.nanmean(e["ER"]),
                lift_mae=np.nanmean(np.abs(e["lift"])), lift_bias=np.nanmean(e["lift"]), fails=nfail)


def run_all(verbose=True):
    """Returns (fits, transfer): lists of dict rows for the CSVs."""
    sets = {name: loader() for name, loader in vs.SETS.items()}
    fits, transfer, cds = [], [], {}
    for name, cases in sets.items():
        rating = cases[0]["kind"] == "rating"
        cd = 1.0
        if rating:
            cd, _ = vs.fit_cd(cases, SCREENING)
        cds[name] = cd
        for mixing in ("CAM", "CPM"):
            p_scr = dict(SCREENING, mixing=mixing)
            m0 = metrics(cases, vs.evaluate(cases, p_scr))
            p_cd = dict(p_scr, Cd_m=cd, eta_d=ETA_D_FIXED)
            names = names_for(name, mixing)
            p_fit, se = vs.fit(cases, p_cd, names)
            m1 = metrics(cases, vs.evaluate(cases, p_fit))
            row = dict(set=name, fluid=cases[0]["fluid"] if name != "Lawrence2012" else "R134a/R1234yf",
                       cls=CLASS[name], kind=cases[0]["kind"], mixing=mixing, n=len(cases),
                       Cd_m=cd, eta_m=SCREENING["eta_m"], fitted=" ".join(names),
                       **{k: p_fit[k] for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                       **{f"se_{k}": se.get(k, float("nan")) for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                       **{f"screen_{k}": v for k, v in m0.items() if k != "n"},
                       **{f"fit_{k}": v for k, v in m1.items() if k != "n"})
            fits.append(row)
            if verbose:
                print(f"{name:16s} {mixing} Cd {cd:.3f} fit eta_s {p_fit['eta_s']:.3f} phi_p "
                      f"{p_fit['phi_p']:.3f} eta_mix {p_fit['eta_mix']:.3f} eta_d {p_fit['eta_d']:.3f}"
                      f" | screening -> fit: "
                      + (f"m_s MAE {m0['m_s_mae']:.1f} -> {m1['m_s_mae']:.1f} %" if rating else
                         f"ER* MAE {m0['er_mae']:.1f} -> {m1['er_mae']:.1f} %, lift MAE "
                         f"{m0['lift_mae']:.1f} -> {m1['lift_mae']:.1f} %"))
    # transfer within the single-phase vapour class
    vap = {n: sets[n] for n in VAPOUR}
    for mixing in ("CAM", "CPM"):
        pbs = {n: dict(SCREENING, mixing=mixing, Cd_m=cds[n], eta_d=ETA_D_FIXED) for n in vap}
        tn = names_for(VAPOUR[0], mixing)
        pooled, se = fit_multi(vap, pbs, tn)
        if verbose:
            print(f"pooled {mixing} on the three vapour sets:",
                  {k: f"{pooled[k]:.3f}+-{se[k]:.3f}" for k in tn})
        for n in vap:
            m = metrics(vap[n], vs.evaluate(vap[n], dict(pbs[n], **pooled)))
            full = dict(pbs[n], **pooled)
            transfer.append(dict(mixing=mixing, fitted_on="all three", predicted=n,
                                 **{k: full[k] for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                                 **{f"se_{k}": se.get(k, float("nan")) for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                                 **m))
        for left in vap:
            train = {n: vap[n] for n in vap if n != left}
            p, se = fit_multi(train, pbs, tn)
            m = metrics(vap[left], vs.evaluate(vap[left], dict(pbs[left], **p)))
            full = dict(pbs[left], **p)
            transfer.append(dict(mixing=mixing, fitted_on=" + ".join(train), predicted=left,
                                 **{k: full[k] for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                                 **{f"se_{k}": se.get(k, float("nan")) for k in ("eta_s", "phi_p", "eta_mix", "eta_d")},
                                 **m))
            if verbose:
                print(f"transfer {mixing}: fit on {list(train)} -> {left}: "
                      + (f"m_s MAE {m['m_s_mae']:.1f} %" if not np.isnan(m['m_s_mae']) else
                         f"ER* MAE {m['er_mae']:.1f} %, lift MAE {m['lift_mae']:.1f} %")
                      + "  (" + "/".join(f"{full[k]:.3f}" for k in tn) + ")")
    return fits, transfer


if __name__ == "__main__":
    run_all()
