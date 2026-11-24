# Ejector validation data: candidate screen

Literature screen, 2026-11-24, for the 1D real-fluid ejector prototype in `ejector/prototype`. No model or data file was changed.
Sources I downloaded are in `docs/ejector/`, which git ignores.

## Summary

- **New CO2 liquid-motive set (best find).** Bodys et al. (2020) publish **150 operating points** from the SINTEF multi-ejector rig.
  - **Motive:** subcooled or saturated liquid CO2 at **47–70 bar**.
  - **Suction:** CO2 vapour at 25–32 bar.
  - **Outlet:** 27–37 bar.
  - **Measured:** motive and suction mass flows for every point.
  - **Where the data are:** the tables are in the journal's **free supplementary PDF**, not in the accepted manuscript. That is why the earlier attempt found nothing.
  - **Geometry:** the motive nozzles are tabulated. The mixing section is published for only one of the two nozzles, and only second-hand (see §2.1).
  - **Use now:** motive-flow validation at the 47 bar target conditions.
  - **Use after one more fact:** ER validation needs the mixer diameter and the mixer used for each point.
- **Liquid-motive / gas-suction.** Witte (1962, TH Delft thesis, open access) tabulates water-jet/air mixing-shock ejector data. The geometry is complete: 30 mm mixing tube, four nozzle area ratios. The data are dimensionless (ε, θ against the Euler number), so they must be converted before use.
- **Other two-phase and single-phase sets that are ready:**
  - Lawrence (2012): R134a/R1234yf, subcooled-liquid motive, 10 tabulated points.
  - Little (2015): R134a/R245fa, wet-to-superheated motive, 19 tabulated points.
  - Falat (2018): R134a vapour ejector, 102 tabulated points with off-design sweeps.
  - Shestopalov et al. (2015): R245fa, 9 geometries at critical mode.
- **Different motive and suction compositions (priority 3): no usable set found.** Hydrogen, steam/air and natural-gas ejector papers either give data only in figures or are paywalled (§4).

How each source was obtained, so the routes can be reused:

| Source | What worked | What failed |
|---|---|---|
| SINTEF/NTNU (Brage → NVA) | `GET https://api.nva.unit.no/publication/<id>/filelink/<fileId>` returns a presigned S3 URL; `<fileId>` is in `associatedArtifacts[]` of `https://api.nva.unit.no/publication/<id>` | `/download/public/...` returns **403** (probably the earlier failure); `sintef.brage.unit.no` no longer resolves |
| Elsevier supplementary files | free at `https://ars.els-cdn.com/content/image/1-s2.0-<PII>-mmc1.pdf` | — |
| MDPI | `https://mdpi-res.com/d_attachment/<journal>/<id>/article_deploy/<id>.pdf` | `www.mdpi.com` returns 403 |
| UIUC IDEALS | `https://www.ideals.illinois.edu/items/<n>.json` lists the files | some dissertations are restricted (Elbel 2007: "Access Denied") |
| Purdue e-Pubs | curl with a browser User-Agent | WebFetch returns 403 |
| Blocked (Cloudflare or 403) | — | AHRI (ARTI reports), CORE downloads, air.uniud.it, flore.unifi.it, DTIC, SSRN |

How the entries were checked:

- **† entries** come from a parallel screening pass. I spot-checked each one against the PDF: geometry values, table locations and sample rows. Condition ranges not explicitly marked as checked were not re-read row by row.
- **All other entries** I read myself.

---

## 1. Ranked table

P = priority class from the brief.

| # | Citation | DOI / URL | Open access | Fluid (motive / suction) | Motive state range | Suction state range | n points | Geometry published | Tabulated or figures | Verdict |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Bodys J., Smolka J., Palacz M., Haida M., Banasiak K. (2020). *Non-equilibrium approach for the simulation of CO2 expansion in two-phase ejector driven by subcritical motive pressure.* Int. J. Refrig. **114**, 32–46 | 10.1016/j.ijrefrig.2020.02.015. Data: `ars.els-cdn.com/content/image/1-s2.0-S0140700720300682-mmc1.pdf` | **Yes.** Accepted manuscript via NVA record `0198cc5dde0d-a689aba8-4c10-4ef2-b70b-4fbb5fa2df77`; supplementary file free | CO2 / CO2 (P1) | 47.4–69.7 bar, 6.3–26.2 °C; subcooled liquid, 0 to about 15 K | 24.8–32.5 bar, −6.6 to 14.3 °C (slightly superheated vapour); P_out 26.7–37.2 bar | **150** (82 nozzle A, 68 nozzle B) | **Partial.** Motive nozzles: manuscript Table 1. Diffuser: Banasiak 2015 Table 2. Mixing section for nozzle A only: Metsue 2024 thesis Table 4.1 (3.25 mm × 16.25 mm). Nozzle B mixing section unpublished | **Tabulated**: supplementary Tables 3–4 (port P and T, P_out, lift, ṁ_mn, ṁ_sn) and Tables 5–6 (motive P, h) | **Ready** for motive flow. **Partial** for ER: needs the mixing section, and the mixer variant used for each point is not stated |
| 2 | Witte J.H. (1962). *Mixing shocks and their influence on the design of liquid-gas ejectors.* PhD thesis, Technische Hogeschool Delft | `resolver.tudelft.nl/uuid:aceb586d-fa3a-4a51-a352-55c7424a98bf` | **Yes** (114-page scan) | water / air with water vapour (P2) | water 10.2–13.3 kg/s, 22 °C, jet Re 7–9·10⁵ | set by the Euler number a = 3.00–23.5 (sub-atmospheric gas pressure) | Tables 7–19, mostly 10 a-values per series and many series (not counted) | **Yes.** D_mt 30 mm, L_mt/D_mt 21 (eq. VI.8). Apparent nozzle diameter D_n 15/16/17/18 mm (φ = A_n/A_mt 0.250/0.284/0.322/0.360). N = 1 (smooth or grooved), 8, 19, 37 or 61 holes. Z_n/D_mt 2–4 | **Tabulated, dimensionless**: "Tables of measured data of the diagrams", pp. 94–104 (ε, θ against a) | **Ready after conversion** to absolute pressures and flows |
| 3 | Lawrence N. (2012). *Analytical and experimental investigation of two-phase ejector cycles using low-pressure refrigerants.* MSc thesis, Univ. of Illinois | `ideals.illinois.edu/items/42285` | **Yes** | R134a, R1234yf; subcooled liquid / vapour (P4) | condenser-outlet liquid, a few K to about 12 K subcooling (Table B.1) | low-temperature evaporator outlet | **10** tabulated (5 + 5); about 200 in figures | **Yes**: Tables 6.1–6.2, p. 64 | **Tabulated**: Table B.1, pp. 102ff (P_mnri, P_lift, M_mnri, M_elri in g/s, enthalpies) | **Ready** |
| 4 | Falat A. (2018). *Étude expérimentale du comportement d'un éjecteur supersonique faisant intervenir un écoulement monophasique du R134a.* MSc thesis, Polytechnique Montréal † | `publications.polymtl.ca/3778` | **Yes** | R134a vapour / vapour (P4) | 2029–2851 kPa, 91.6–106.3 °C (about 20 K superheat) | 285–416 kPa, 12–27 °C; P_out 496–919 kPa, swept through critical | **102** | **Yes**: Table 1 and Fig. 2 (p. 20) | **Tabulated**: Appendix A (pp. 80–83). P/T at all three ports, wall tap, FT3 + FT4 (suction) and FT5 (motive) | **Ready** |
| 5 | Little A.B. (2015). *An understanding of ejector flow phenomena for waste heat driven cooling.* PhD thesis, Georgia Tech | Georgia Tech SMARTech repository | **Yes** | R134a and R245fa; motive from 10–12 K superheat down to wet (x = 0.73 / 0.84) (P4) | R134a 2681–3104 kPa; R245fa 1375–1604 kPa † | R134a evaporator 469 kPa; R245fa 90 kPa; both about +10 K superheat; condenser 765 / 214 kPa † | **11 + 8** | **Yes**: Fig. 5.2, p. 127, dimensioned drawing (checked) | **Tabulated**: Tables 5.1–5.2, pp. 128–129. P, T, h_in, COP, Q_evap. Motive flow fixed at 0.85 / 0.48 kg/min † | **Ready with derivation**: ṁ_s = Q_evap/Δh_evap, P_out ≈ condenser pressure |
| 6 | Shestopalov K.O., Huang B.J., Petrenko V.O., Volovyk O.S. (2015). *Investigation of an experimental ejector refrigeration machine operating with refrigerant R245fa at design and off-design working conditions. Part 2.* Int. J. Refrig. **55**, 212–223 (Part 1: 201–211) † | 10.1016/j.ijrefrig.2015.02.004 | **Yes** (National Taiwan University repository copy) | R245fa vapour (P4) | T_g 90 / 95 / 100 °C (saturation only; motive superheat not stated) | T_e 8 / 12 / 16 °C, 5–7 K superheat | **9** critical points (Table 7), plus 9 with conical-cylindrical mixers (Table 8) | **Yes**: Table 1 (3 nozzles), Table 2 (3 constant-area chambers), Table 6 (NXP) | **Tabulated** critical u*, Tc*, Qe* (Table 7); off-design in figures | **Ready** for critical mode; off-design needs digitising |
| 7 | Metsue A. (2024). *Advanced numerical and thermodynamic modeling of CO2 ejectors…* PhD thesis, UCLouvain / Univ. de Sherbrooke. Journal version: Metsue, Nesreddine, Bodys, Poncet, Bartosiewicz (2024), Energy **311**, 133222 | Thesis: `research.dial.uclouvain.be/bitstreams/3b8bd8d2-f60d-4177-8bc9-7ff41cca88fd/download`. Journal: 10.1016/j.energy.2024.133222 | Thesis **yes**; journal **no** (not opened) | CO2 / CO2, transcritical (P1) | about 86.1 bar, 35.5 °C ("slight variation from case to case") | about 28.0 bar, 7.5 °C | **5** | **Yes**: thesis Table 4.1 (p. 82), full axisymmetric geometry of the SINTEF ejector "provided by" Bodys et al. (private communication) | **Tabulated**: thesis Table 4.3 (p. 86): P_out, measured ṁ_p and ṁ_s | **Ready** (small). Main value is the geometry it supplies for #1 |
| 8 | Fabris F., Pardiñas Á.Á., Marinetti S., Rossetti A., Hafner A., Minetto S. (2023). *A novel R744 multi-temperature cycle for refrigerated transport applications with low-temperature ejector…* Int. J. Refrig. **152**, 26–35 | 10.1016/j.ijrefrig.2023.05.003; `zenodo.org/records/8108455` | **Yes** (submitted version on Zenodo) | CO2 / CO2 (P1) | 54.5 bar / 15.1 °C (liquid); 66.7 bar / 24.9 °C; 89.7 bar / 34.5 °C | 16.8 / 19.7 / 22.9 / 26.5 / 30.5 bar (saturation −25 to −5 °C); lift raised in 1 bar steps from about 2 bar | not stated | **Partial**: Danfoss Multi Ejector VEJ1, throat 1.00 mm. Paper refers to Banasiak 2015 for geometry; mixing section unpublished | **Figures**: Figs 4–6 (ER, efficiency against lift). Table 3 gives the mean motive state and flow for each of the 3 motive conditions | **Motive flow ready** (3 averaged states; VEJ1 nozzle 1.00 / 1.12 mm from Banasiak 2015 Table 2). **ER needs digitising and the mixer diameter** |
| 9 | Ameur K., Aidoun Z. (2018). *Nozzle displacement effects on two-phase ejector performance: an experimental study.* J. Appl. Fluid Mech. **11**(4), 817–823 | 10.29252/jafm.11.04.28385 | **Yes** | R134a; subcooled liquid / vapour (P4) | 8.8–14.9 bar, subcooling 0.2 / 1 / 5 K † | about 10 K superheat; P_out 3.7–4.7 bar † | figures only | **Yes**: Table 1. D_t 1.39, D_out 4.09, L_div 38 mm; mixing section 6.9 × 164.3 mm; diffuser 134.26 mm to Ø31.8 mm | **Figures** (Figs 5–7) | **Needs digitising** |
| 10 | Palacz M., Haida M., Smolka J., Nowak A.J., Banasiak K., Hafner A. (2017). *HEM and HRM accuracy comparison for the simulation of CO2 expansion in two-phase ejectors for supermarket refrigeration systems.* Appl. Therm. Eng. **115**, 160–169 | 10.1016/j.applthermaleng.2016.12.122 | **Yes** (accepted version via NVA) | CO2 / CO2 (P1) | 47.8–94.5 bar, 6.3–35.3 °C; operating regimes 1–9 are subcritical liquid | 27.2–32.0 bar, 0.5–8.5 °C; P_out 31.0–38.5 bar | **18** | **Partial**: motive nozzles of EJ1–EJ4 (Table 1). Mixing section unpublished, and no ejector is assigned to each regime | **Tabulated**: Table 2 (port P and T, P_out, ṁ_MN, ER) | **Partial.** Same rig as #1; several regimes duplicate #1 |
| 11 | Toffoletti G., Barta R., Grajales S.M., Liu H., Ziviani D., Groll E.A. (2025). *Experimental comparison of cycle modifications and ejector control methods using variable geometry and CO2 pump…* Int. J. Refrig. **169**, 226–240 | 10.1016/j.ijrefrig.2024.10.001; `docs.lib.purdue.edu/mepubs/62` | **Yes** (CC BY-NC-ND) | CO2 / CO2 (P1) | **pumped liquid**: pump outlet 74.8–95.5 bar from gas-cooler outlet at about 70 bar (19 °C ambient, Table 6). Also transcritical runs | MT evaporator, about 24–29 bar (Table 7) | 5 pump points, 53 cycle points in total | **Partial**: Liu & Groll adjustable-needle ejector, drawings in Appendix A Figs A.1–A.4. The effective throat is set by the needle | **Figures** (Figs 12–18). Tables 6–7 give cycle data without ejector flows | **Needs digitising**; throat ambiguous |
| 12 | Haberschill P., Nehdi E., Kairouani L., Elakhdar M.A. (2021). *Experimental study of a two-phase ejector for CO2 transcritical refrigeration system.* Archives of Thermodynamics **42**(4), 217–246 | 10.24425/ather.2021.139660 | **Yes** | CO2 transcritical (P1) | 75–92.7 bar; T_GC 30–66 °C, varying and not given per point | evaporation −10 to +3 °C | Tables 5–9, about 15 cells each | **Yes**: Tables 2–4. Throat 0.9–1.2 mm, exit 1.24–1.54 mm, mixing section Ø2.7–5 mm × 12.5–40 mm, diffuser 7° to Ø8 mm | ER and compression ratio **tabulated** against NXP; motive T, suction P and flows not per point (flows in Figs 8–10) | **Partial**: inlet states underdetermined |
| 13 | Ameur K., Aidoun Z., Falsafioon M. (2020). *Experimental performance of a two-phase ejector: nozzle geometry and subcooling effects.* Inventions **5**(2), 23 † | 10.3390/inventions5020023 | **Yes** (mdpi-res.com) | R134a liquid / vapour (P4) | subcooling 0.5–46 K | P_out 3–7 bar | figures | **Partial**: nozzles in Table 2; body in Fig. 3 (not inspected) | **Figures** (Figs 5–9) | **Needs digitising** |
| 14 | Nakagawa M., Berana M.S., Kishine A. (2009). *Supersonic two-phase flow of CO2 through converging–diverging nozzles for the ejector refrigeration cycle.* Int. J. Refrig. **32**, 1195–1202 | 10.1016/j.ijrefrig.2009.01.015 | No (not opened) | CO2, **nozzle only** (P1, secondary) | 6.1 MPa, 20–21.8 °C, subcooled liquid (from Metsue thesis Table 2.1) | — | 3 nozzles | rectangular nozzles (dimensions in Metsue Table 2.1) | wall-pressure profiles | **Secondary** (nozzle pressure path only); **needs full text** |
| 15 | Sanger N.L. (1968). NASA TN reports on low-area-ratio water jet pumps, throat lengths 7.25 D and 5.66 D | NTRS 19680008095, 19680022488 | **Yes** | water / water (liquid–liquid) | primary flow 28–83 gpm | secondary flow varied | figures | **Yes**: throat 1.35 in (3.43 cm), L = 7.25 D or 5.66 D, area ratio R = 0.066 / 0.197, diffuser 8° | Table I gives ranges only; results in figures | **Needs digitising**; liquid–liquid limit only |
| 16 | Xu X., Mou J., Zhang H., et al. (2023). *Experimental study on performance of liquid–gas jet pump with square nozzle.* Energies **16**, 7951 | 10.3390/en16247951 | **Yes** | water / air (P2) | pump-fed water; Q_n 50 m³/h class pump | inlet air pressure P_a varied (best efficiency at 60 kPa per abstract) | figures | **Partial**: Table 1 structural parameters (square nozzle, area ratio 2.25). Text extraction garbled; not checked | **Figures** | **Needs digitising**; large but non-circular nozzle |
| 17 | Bodys J., Smolka J., Palacz M., Haida M., Banasiak K., Nowak A.J. (2021). *Experimental and numerical study on the R744 ejector with a suction nozzle bypass.* Appl. Therm. Eng. **194**, 117015 | 10.1016/j.applthermaleng.2021.117015 | **Yes** (accepted version via NVA) | CO2 transcritical | 81–91 bar, 29–39 °C | about 28 bar | 1 point with flows (Table 4) | **Partial**: Table 2. Throat 1.4 mm, D_MIX 6.0 mm, L_MIX 16.0 mm, D_DIF 8.4 mm, 5°. **No nozzle exit diameter** | Table 3 gives conditions only | **Unusable** as a data set |

---

## 2. Top candidates in detail

### 2.1 Bodys et al. 2020: CO2, subcooled liquid motive 47–70 bar, 150 points (rank 1)

**Where the data are.** The accepted manuscript on NVA, saved as `docs/ejector/Bodys2020_IJR114_accepted.pdf`, has **no appendices**. The 150 operating conditions are in the Elsevier supplementary file `1-s2.0-S0140700720300682-mmc1.pdf`, saved as `docs/ejector/Bodys2020_IJR114_mmc1.pdf` (4 pages).

The table numbers differ between the two documents:

| Content | In the supplementary file | Cited in the manuscript text as |
|---|---|---|
| Experimental data, nozzle A | Table 3 (Appendix A) | Table 3 |
| Experimental data, nozzle B | Table 4 (Appendix B) | Table 5 |
| Motive P and h with accommodation coefficient | Tables 5–6 (Appendix A2/B2) | Tables 4 and 6 |

**Geometry.**

- **Motive nozzles** (manuscript Table 1, p. 11; the pdftotext columns are shifted, so read the page):
  - Inlet diameter 3.80 mm for both nozzles.
  - Throat 1.41 mm (A) and 2.00 mm (B).
  - Outlet 1.58 mm (A) and 2.24 mm (B).
  - Converging angle 30°, diverging angle 2°.
- **These nozzles are the SINTEF/Danfoss cartridges VEJ2 and VEJ3.** Banasiak et al. 2015 (IJR 57:265, Table 2) gives the same nozzle values for VEJ2 and VEJ3. It also gives the diffuser outlet (8.4 mm and 10.3 mm) and the diffuser angle (5°).
- **Mixing section.** Bodys 2020 says only that "the remaining dimensions were established on the basis of aforementioned studies". The SINTEF papers I checked (Banasiak 2015, Palacz 2017, Haida 2018, Bodys 2018) all omit the mixer diameter.
- **Mixer for the nozzle-A ejector.** Metsue (2024) thesis Table 4.1 (p. 82) gives a full geometry for "a single ejector geometry provided by [Bodys et al., private communication]… [that] corresponds to the experimental setup of Haida et al.":
  - motive nozzle inlet 3.80 mm, throat 1.41 mm, exit 1.58 mm;
  - **constant-area duct 3.25 mm diameter × 16.25 mm long**;
  - diffuser 5°, outlet 8.40 mm.

  These match VEJ2 exactly, so this is the nozzle-A mixer. The journal version (Energy 311:133222, with Bodys as co-author) was not opened.

**Conditions** (extracted from the supplementary Tables 3–4):

| | Nozzle A (VEJ2) | Nozzle B (VEJ3) |
|---|---|---|
| Points | 82 | 68 |
| P_mn [bar] | 52.8–68.9 | 47.4–69.7 |
| T_mn [°C] | 6.3–26.1 | 8.2–26.2 |
| P_sn [bar] | 25.7–32.5 | 24.8–29.9 |
| T_sn [°C] | −6.6 to 7.3 | −1.9 to 14.3 |
| P_out [bar] | 26.9–37.2 | 26.7–36.7 |
| Lift [bar] | 1.0–8.9 (13 points > 4 bar) | 1.7–8.2 (20 points > 4 bar) |
| ṁ_mn [kg/s] | 0.053–0.107 | 0.082–0.175 |
| ṁ_sn [kg/s] | 0.0002–0.055 | 0–0.077 |
| Points with P_mn < 55 bar | 4 | 12 |

The paper gives the motive subcooling as 0 to about 15 K. Measurement accuracy (manuscript p. 12, citing Haida 2016) is 0.05–0.3 K, ±0.3 % of reading for pressure and ±0.2 % of reading for mass flow.

**What it tests.**

- Choked liquid-CO2 motive flow from 47 to 70 bar: the HEM-vs-delayed-flashing question at the 47 bar application point. The paper's own premise is that HEM and HRM are unsatisfactory in this range. The tabulated accommodation coefficients (supplementary Tables 5–6) are a ready comparison for the prototype's φ factor.
- **Large scale**: nozzle B has a 2.00 mm throat.
- ER and lift at suction 25–32 bar, once the mixer is fixed.

**Caveats.**

1. **Mixer variant per point is not stated.** "Each pair contains the ejector for the low and the high pressure lift". The manuscript (Figs. 3–4) groups points as lift below 4 bar or above 4 bar and marks "the types of mixing chambers" by those groups. Inferring the mixer from lift is therefore my reading, not a stated mapping.
2. **The 3.25 mm mixer may be only one of the two VEJ2 mixers.** Metsue's validation points have a lift of about 4.8–10 bar, which suggests the high-lift variant. This is unconfirmed.
3. **The VEJ3 mixer is unpublished.**
4. **Multi-ejector rack**: ejectors run in parallel in a common block.

**Recommended action:** ask the authors or SINTEF for the VEJ2 and VEJ3 low- and high-lift mixer dimensions, and for the mapping from operating point to mixer. Until then, use all 150 points for motive flow, and the nozzle-A high-lift points (13) for a provisional ER check.

**Extraction QA.** I extracted the tables with PyMuPDF word coordinates; pdftotext misaligns the columns. Checks:

- 150 of 150 rows parsed.
- Lift equals P_out − P_sn on every row (largest residual 0.049 bar; lift is tabulated to 0.1 bar).
- Motive P matches the authors' Tables 5–6 on all 150 rows.
- CoolProp h(P, T) matches their tabulated h within **0.06 kJ/kg** on all 150 rows.

The CSV is in **Appendix A**. Independent check (2026-11-24): all 150 rows match exactly one line of the supplementary PDF, number for number, with columns in the order of the printed table header; Table 1 nozzle diameters confirmed on the manuscript page.

### 2.2 Witte 1962: water-jet / air liquid–gas ejector with mixing shocks (rank 2)

**Geometry** (Ch. VI):

- **Mixing tube:** cylindrical, D_mt = 30 mm, L_mt/D_mt = 21, constant in all runs (eq. VI.8).
- **Nozzle sets:** four, with apparent diameter D_n = 15, 16, 17 and 18 mm, giving φ = A_n/A_mt = 0.250, 0.284, 0.322 and 0.360.
- **Hole patterns:** single-hole (smooth or grooved) and multi-hole with N = 8, 19, 37 or 61 holes. D_kn/D_mt = 0.666 or 0.800.
- **Nozzle spacing:** Z_n/D_mt = 2, 3 or 4.
- **Water flow:** M_l = 10.2–13.3 kg/s at 22 °C.
- **Diffuser:** measurements in Ch. VI §4. I did not extract its geometry.

**Data.** "Tables of measured data of the diagrams", Tables 7–19 on pp. 94–104.

- Each series sets the Euler number a (3.00–23.5) and gives the measured ε (gas pressure ratio across the mixing tube) and θ (gas-to-liquid volume-flow ratio).
- Series vary φ, N, Z_n/D_mt and Re.
- The back pressure was adjusted in every run so that the mixing zone ended at the mixing-tube exit (Ch. VI §2). The data are therefore points on the mixing-shock locus, not free off-design sweeps.

**What it tests:**

- liquid-motive, gas-suction, constant-area mixing;
- the subsonic post-shock root;
- the effect of area ratio;
- water-vapour content in the gas.

The 1969 JFM paper (36:639) is the paywalled short version.

**Caveats:**

- Converting to absolute pressures and flows needs the exact definitions of a, ε and θ (symbol list p. 105, Ch. II–VI) and the per-series M_l and D_n.
- The scan's OCR is imperfect (for example "2.1 1"), so transcribe from page images.
- The motive nozzle is multi-hole. Use the total hole area as A_t = A_e (converging, liquid).

### 2.3 Lawrence 2012: R134a / R1234yf two-phase ejector, subcooled-liquid motive (rank 3)

**Geometry** (Tables 6.1–6.2, p. 64). The modular ejector has four motive nozzles and four mixing sections:

| Nozzle | Throat [mm] | Diverging angle (full) | Diverging length [mm] | Outlet [mm] |
|---|---|---|---|---|
| 1 | 0.8 | 2.3° | 45 | 2.7 |
| 2 | 1.0 | 2.3° | 40 | 2.7 |
| 3 | 1.0 | 2.3° | 20 | 1.8 |
| 4 | 1.3 | 2.3° | 35 | 2.7 |

- Mixing sections, D × L: 3 × 25, 5 × 60, 5 × 40 and 6 × 70 mm.
- The extracted L/D column does not line up with D × L. Read Table 6.2 from the page image.

**Data.** Table B.1 (pp. 102ff) gives 10 points (5 R134a, 5 R1234yf) with:

- the 1.0 mm throat / 40 mm diverging length / 2.7 mm outlet nozzle and the 5 × 60 mm mixer;
- motive inlet pressure P_mnri, condenser-outlet subcooling, P_lift and port enthalpies;
- motive and suction mass flows M_mnri and M_elri in g/s. Motive flow is about 12–13 g/s and suction about 1–2 g/s, so ER is low.

The roughly 200 other points, including the geometry-variation results in Ch. 7, are figure-only.

**What it tests:** the same physics as the CO2 target (subcooled-liquid motive flashing in the nozzle), with low-pressure refrigerants. It adds a second and third pure fluid.

**Caveat:** small ER, so this mainly tests motive flow and lift. The journal version (IJR 38, 2014, 310–322) was not opened.

### 2.4 Falat 2018: R134a supersonic vapour ejector, 102 points (rank 4) †

**Geometry** (Table 1 and Fig. 2, thesis p. 20; I checked the throat, exit, mixing and NXP values against the text):

- **Motive nozzle:** throat 7.09 mm, exit 9.70 mm, convergent angle 17.7°.
- **Mixing section:** constant-area Ø16.4 mm × 89.0 mm.
- **Nozzle exit position:** 29.0 mm to the mixing-chamber entrance.
- **Diffuser:** 315 mm long to Ø50.4 mm (3.1° per the screening pass).

**Data.** Appendix A (pp. 80–83), "Tableaux des données expérimentales", five test series. Each row has:

- P2/T2 (motive), P1/T1 (suction), P4/T4 (outlet) and a wall tap;
- Omega (%) and Coriolis flows FT3, FT4 and FT5 in g/s.

Primary flow = FT5 and secondary = FT3 + FT4 (text p. 21). In the extracted text the **Omega column is shifted down one row**: for row 1 of series #1, (FT3 + FT4)/FT5 = 40.85 %, the value printed on row 2. Each series sweeps P4 into backflow (FT3 → 0), so the set covers the critical, subcritical and backflow regimes.

**What it tests:** real-gas single-phase rating and off-design maps at large scale (7 mm throat). This complements Huang 1999, which is critical-point only.

**Caveat:** single CanmetENERGY geometry. Poirier et al. (Energy 162, 2018) used the same rig with a 5.63 mm throat, but that paper is paywalled.

### 2.5 Little 2015: R134a and R245fa ejectors, superheated to wet motive (rank 5)

**Geometry** (Fig. 5.2, p. 127, checked on the page image):

| | R134a | R245fa |
|---|---|---|
| Motive throat [mm] | Ø1.19 | Ø1.17 |
| Motive exit [mm] | Ø1.88 | Ø2.67 |
| Mixing section [mm] | Ø3.00 × 30.00 | Ø4.11 × 28.80 |
| Diffuser | 125.90 mm to Ø13.58 | 92.93 mm to Ø13.44 |
| Axial dimension upstream of the mixing section, as drawn [mm] | 53.21 | 52.02 |

**Data.** Tables 5.1 (R134a) and 5.2 (R245fa), pp. 128–129, give per point:

- motive superheat (or quality), boiler P, saturation temperature, motive T and h_in;
- COP and Q_evap.

Held fixed, per the screening pass and the text on p. 127: motive flow at the critical value (0.85 kg/min R134a, 0.48 kg/min R245fa), evaporator and condenser pressures. Appendix C gives a sample state-point calculation.

**What it tests:** HEM on a **wet motive** (x down to 0.73), plus a second geometry and fluid.

**Caveat:** derive ṁ_s from Q_evap and the evaporator enthalpy rise, and take P_out as the condenser pressure. Neither is directly measured.

### 2.6 Shestopalov et al. 2015: R245fa, 3 nozzles × 3 mixing chambers (rank 6) †

**Geometry** (Part 2, checked against the text):

- **Nozzles N1–N3** (Table 1):
  - throat 4.515 / 4.212 / 3.902 mm;
  - exit 7.8 / 7.11 / 6.412 mm;
  - diverging length 31.48 / 28.33 / 24.71 mm.
- **Cylindrical mixing chambers A–C** (Table 2):
  - Ø12.155 / 13.020 / 14.010 mm;
  - length 90.62 / 98.64 / 106.65 mm;
  - diffusers to Ø32 mm.
- **Optimal NXP:** Table 6.

**Data.** Table 7 gives the critical condensing temperature Tc*, critical ER u* and Qe* for the nine ejectors 1-A … 3-C at T_g/T_e = 90/8, 95/12 and 100/16 °C.

**What it tests:** critical ER and critical back pressure across nine area ratios with one fluid. This is a scaling test similar to Huang 1999.

**Caveat:** conditions are given as saturation temperatures; motive superheat is not stated (assume saturated vapour or check Part 1). Off-design curves (Figs 11 and 14) need digitising.

### 2.7 Fabris et al. 2023: CO2 low-temperature ejector, liquid motive at 54 bar (rank 8)

**Why it matters.** Its 54.5 bar / 15.1 °C liquid motive with suction at 16.8–22.9 bar is the closest published match to the 47 → 15 bar target.

- **Ejector:** Danfoss Multi Ejector CTM, smallest cartridge VEJ1, throat 1.00 mm (Sec. 3).
- **Rig:** SINTEF SuperSmart-Rack.
- **Test matrix:** Table 2.
- **Mean measured motive states and flows** (Table 3, checked on the page image). With the VEJ1 nozzle from Banasiak 2015 Table 2 (inlet 3.8 mm, throat 1.00 mm, outlet 1.12 mm, 30°/2°), these three rows are a ready motive-flow check:

  | P [bar] | T [°C] | ṁ [kg/min] |
  |---|---|---|
  | 89.70 | 34.53 | 2.688 |
  | 66.66 | 24.92 | 2.173 |
  | 54.52 | 15.05 | 2.736 |

- **ER and efficiency against lift:** Figs 4–6.
- **Finding at 54 bar:** "not able to entrain mass flow rate from the lowest suction pressure (16.8 bar)". That boundary is a useful qualitative test of the model at the target.

**Caveats:**

- The VEJ1 mixer is unpublished; Banasiak 2015 Table 2 gives only the nozzle and diffuser.
- Only the submitted version on Zenodo was read.
- The data need digitising.

### 2.8 Ameur & Aidoun 2018: R134a two-phase, CanmetENERGY (rank 9)

**Geometry** (Table 1, checked):

- **Motive nozzle:** D_t 1.39 mm, D_out 4.09 mm, L_div 38 mm.
- **Mixing section:** Ø6.9 mm × 164.3 mm.
- **Diffuser:** 134.26 mm to Ø31.8 mm.

**Conditions** (†): subcooled-liquid motive at 8.8–14.9 bar with 0.2–5 K subcooling, P_out 3.7–4.7 bar, NXP from −10 to 44.4 mm. Flows and ER are in Figs 5–7 only.

**What it tests:** near-saturated liquid motive and large mixing length L/D (about 24). The companion paper (Ameur et al. 2020, Inventions) adds subcooling up to 46 K, also figure-only.

---

## 3. Rejected candidates

- **Gullo, Birkelund, Kriezi, Kærn (2021), ECM 237:114082, plus DTU data repository xlsx** (figshare 16716349). The raw data are open, but the geometry table (Table 1: 0.71 / 0.79 mm nozzle, 7.3 mm diffuser outlet) has no mixer; transcritical, PWM-pulsed flow.
- **Banasiak et al. (2015), IJR 57:265.** Geometry source only. Multi-ejector nozzle and diffuser table (Table 2), no mixer, mapping functions rather than points.
- **Haida et al. (2018), Energy 153:933.** Same geometry table as Banasiak 2015; reduced-order-model coefficients, not raw points.
- **Bodys et al. (2018), IJR 90:216.** CFD; dimensioned baseline ejector (Fig. 2) with the mixer given only in dimensionless form.
- **Bodys et al. (2021), ATE 194:117015.** Only one tabulated point with flows; no motive exit diameter (see #17).
- **Xu K. (2023), MASc thesis, Univ. of Victoria (PEMFC nested-nozzle H2 ejector).** Geometry in Tables 3–4, but the tests used air and the results are figures only.
- **Baxter A.M. (Glasgow PhD), "Air entrainment by fluid jets and streams".** Scanned thesis with unreadable OCR; old apparatus; not pursued.
- **Wang P. et al. (2023), IEA HPC, OSTI 1976028.** 1D model only.
- **Mohammed et al. (2023), IEA HPC, OSTI 1976014** †. Theoretical.
- **Śmierciew et al. (2022), Sustainability 14:13356, R600a** †. Data reduced to velocity coefficients and nozzle flux (Figs 4–5); no per-point ejector table.
- **Pereira et al. (2015), Front. Mech. Eng. 1:7, R600a spindle ejector** †. Figures only; effective throat varies with the spindle.
- **Lamberts (2018), UCLouvain PhD, air** †. One tabulated point; its Table 5.1 is the Huang 1999 set already in use.
- **Falsafioon, Aidoun, Ameur (2019), JAFM 12(6):2003** †. CFD; tables embedded as images.
- **Scott et al. (2008), NRCan CFD of R245fa** †. Eight Ablwaifa (2006) points, but no geometry for that ejector.
- **Hart (2002), Sheffield PhD** †. CFD of the Eames steam ejector; nozzle radii assumed.
- **Dudar (2013), Arch. Thermodyn.; Lopes (2013), Porto MSc; Menegay (1997), VT PhD** †. Theoretical, COP-only, or mainly computational.
- **Metsue thesis use of Zhu et al. (2017), ECM 151:98.** Figure-only ER curves; the original was not opened (see §4).

## 4. Not verified (could not access, or only the abstract)

- **Liu F., Groll E.A. (2008). *Recovery of throttling losses by a two-phase ejector in a vapor compression cycle*, ARTI Report 10110-01** (AHRI `ahrinet.org/system/files/2023-06/ARTI-Rpt-10110-01_0.pdf`). Cloudflare 403. Likely full drawings and data for the Purdue adjustable CO2 ejector also used by Toffoletti 2025. **Worth fetching in a browser.**
- **Elbel S. (2007). UIUC PhD dissertation (IDEALS item 85178).** Restricted ("Access Denied"); transcritical R744.
- **Lucas C., Rusche H., Schroeder A., Koehler J. (2014). IJR 43:154–166.** Not opened.
- **Nakagawa M. et al. (2011). IJR 34:1604–1613 (mixing length, CO2).** Not opened.
- **Banasiak K., Hafner A., Andresen T. (2012). IJR 35:1617–1625 (geometry variants).** No open file on NVA.
- **Zhu Y., Li C., Zhang F., Jiang P.-X. (2017). ECM 151:98–106 (transcritical CO2).** Per Metsue: converging-only nozzle, A_y/A_t = 6.25, separate motive and suction flows. Not opened.
- **Nakagawa et al. (2009)** (rank 14). Values seen only via Metsue Table 2.1.
- **Metsue et al. (2024), Energy 311:133222.** Paywalled; SSRN preprint 10.2139/ssrn.4860890 returned 403. Assumed to contain the same Table 4.1 as the thesis.
- **Palacz M. et al. (2016), ATE 95:62 (CFD mixer shape optimisation of a SINTEF ejector).** No open file; might state the baseline mixer diameter.
- **Ringstad K.E. (2023), NTNU PhD "CFD Modelling for Improved Components in CO2 and Ammonia Vapour Compression Systems"** (NVA record, file status unclear), and **Ringstad et al. (2021), ATE 199:117604** (open access, CFD/ML).
- **Cunningham R.G. (1974), J. Fluids Eng. 96:203** (already known; paywalled); **Witte (1969), JFM 36:639** (short version of #2).
- **Holton W.C. (1951), Trans. ASME 73:905, DOI 10.1115/1.4016494.** Effect of entrained-gas molecular weight on steam-jet ejectors; paywalled. The classic different-composition reference.
- **Nikiforow K. et al. (2016), IJHE 41:14952–14970 (5 kW PEMFC H2 ejector, tested with air and humid H2).** VTT CRIS has no full text.
- **Li et al. (2017), IJHE 42:29966–29970 (water-vapour effect on a subsonic PEMFC ejector).** Abstract only: ER 2.3–2.8 with H2 saturated at 60 °C.
- **Mazzelli & Milazzo (2015), IJR 49:79–92 (R245fa); Mazzelli, Little, Garimella, Bartosiewicz (2015), IJHFF 56:305 (air)** †. FloRe and publisher blocked.
- **García del Valle et al. (2014), IJR 46:105 (R134a); Poirier et al. (2018), Energy 162:1287; Ameur, Aidoun, Ouzzane (2018), ETFS 97:12; Chaiwongsa & Wongwises (2007/2008)** †. Paywalled.
- **Steam ejectors with geometry and flows:** Sriveerakul et al. (2007), IJTS 46:812; Chunnanond & Aphornratana (2004); Varga et al. (2011), IJR 34:1668; Eames et al. (1995), IJR 18:378 †. No open copy found.
- **Natural-gas ejector field or lab data with geometry:** none found beyond the Liao (2008) thesis already in hand.
- **No ammonia (R717) or water flashing-ejector set** with full geometry and measured flows turned up.

---

## Appendix A. Bodys et al. 2020: supplementary Tables 3–4, machine-extracted

**Source:** `docs/ejector/Bodys2020_IJR114_mmc1.pdf` (Elsevier supplementary file mmc1 to doi:10.1016/j.ijrefrig.2020.02.015).

- Nozzle A = motive nozzle A (VEJ2, throat 1.41 mm). Nozzle B = motive nozzle B (VEJ3, throat 2.00 mm).
- `oc` is the authors' operating-condition number.
- Units: bar, °C, kg/s. `lift_bar` = P_out − P_sn, as tabulated.
- The checks in §2.1 apply. Spot-check against the PDF before committing as a reference data file.

```csv
nozzle,oc,P_mn_bar,T_mn_C,P_sn_bar,T_sn_C,P_out_bar,lift_bar,m_mn_kgs,m_sn_kgs
A,01,68.899,25.772,29.477,1.096,32.860,3.4,0.0598,0.0313
A,02,68.843,26.014,26.566,2.396,30.500,3.9,0.0597,0.0228
A,03,68.563,26.136,29.825,2.421,33.543,3.7,0.0593,0.0236
A,04,68.450,26.077,27.652,-0.181,30.965,3.3,0.0590,0.0359
A,05,68.230,26.010,29.155,1.247,32.290,3.1,0.0591,0.0380
A,06,68.196,25.920,26.487,0.084,29.925,3.4,0.0593,0.0315
A,07,67.976,26.080,26.605,0.581,30.144,3.5,0.0591,0.0298
A,08,67.976,25.941,27.697,-0.397,31.152,3.5,0.0590,0.0311
A,09,67.815,25.885,27.816,-0.258,31.572,3.8,0.0590,0.0236
A,10,67.575,20.177,28.358,3.340,36.768,8.4,0.0875,0.0054
A,11,67.316,24.953,28.626,3.828,31.909,3.3,0.0590,0.0302
A,12,66.620,22.378,27.865,1.777,32.875,5.0,0.0723,0.0220
A,13,66.594,25.020,28.257,3.140,31.787,3.5,0.0585,0.0226
A,14,66.509,22.405,28.208,2.207,34.852,6.6,0.0722,0.0138
A,15,66.492,24.916,29.518,3.821,32.696,3.2,0.0583,0.0296
A,16,66.424,25.153,26.522,1.951,29.655,3.1,0.0579,0.0333
A,17,66.201,24.937,29.743,4.456,32.764,3.0,0.0580,0.0339
A,18,66.144,24.987,29.309,2.962,32.814,3.5,0.0579,0.0223
A,19,66.101,25.011,25.997,0.413,29.415,3.4,0.0582,0.0262
A,20,66.053,24.139,29.524,1.277,32.599,3.1,0.0605,0.0308
A,21,65.910,24.880,26.692,1.631,30.185,3.5,0.0579,0.0241
A,22,65.828,24.701,27.131,0.916,30.218,3.1,0.0577,0.0335
A,23,65.706,24.235,26.531,1.812,29.371,2.8,0.0577,0.0354
A,24,65.603,23.947,26.246,3.052,29.341,3.1,0.0590,0.0304
A,25,65.082,24.119,26.177,0.751,29.704,3.5,0.0574,0.0198
A,26,64.901,24.111,27.455,0.452,30.340,2.9,0.0569,0.0334
A,27,64.756,24.063,29.272,1.702,32.474,3.2,0.0580,0.0236
A,28,64.646,23.380,27.880,0.863,31.171,3.3,0.0607,0.0251
A,29,64.574,23.189,27.730,1.504,30.470,2.7,0.0607,0.0371
A,30,64.397,24.244,29.032,1.289,31.881,2.8,0.0568,0.0322
A,31,64.090,23.092,27.533,0.976,30.584,3.1,0.0609,0.0294
A,32,63.983,23.307,29.227,1.748,32.209,3.0,0.0587,0.0279
A,33,63.127,22.808,29.079,1.149,31.650,2.6,0.0589,0.0367
A,34,62.740,22.919,26.387,1.573,29.371,3.0,0.0581,0.0265
A,35,62.693,22.785,29.462,4.178,32.334,2.9,0.0597,0.0280
A,36,62.591,23.155,26.273,-0.398,29.026,2.8,0.0548,0.0325
A,37,62.539,22.762,26.416,1.386,29.219,2.8,0.0584,0.0306
A,38,62.369,22.203,29.260,1.614,31.967,2.7,0.0609,0.0323
A,39,62.367,21.145,27.669,2.074,30.595,2.9,0.0704,0.0311
A,40,62.323,21.782,29.596,1.762,32.520,2.9,0.0650,0.0288
A,41,62.199,21.058,29.264,1.496,31.329,2.1,0.0743,0.0476
A,42,62.184,21.147,29.203,1.696,31.747,2.5,0.0696,0.0383
A,43,62.134,21.684,29.369,3.543,31.870,2.5,0.0633,0.0375
A,44,62.111,21.017,27.735,-0.326,30.437,2.7,0.0704,0.0355
A,45,61.886,20.961,27.587,-0.274,30.076,2.5,0.0696,0.0387
A,46,61.793,20.272,29.930,3.580,33.871,3.9,0.0721,0.0187
A,47,61.519,21.952,28.239,1.651,31.058,2.8,0.0596,0.0287
A,48,61.422,20.809,29.149,1.909,31.866,2.7,0.0704,0.0336
A,49,61.228,22.372,26.266,0.228,29.172,2.9,0.0542,0.0273
A,50,60.965,22.481,26.247,0.058,29.003,2.8,0.0537,0.0308
A,51,60.923,22.148,26.297,0.408,29.292,3.0,0.0539,0.0248
A,52,60.630,19.986,25.939,-0.430,28.294,2.4,0.0726,0.0379
A,53,60.474,19.816,26.203,1.327,28.813,2.6,0.0738,0.0341
A,54,60.401,19.779,25.873,0.520,27.785,1.9,0.0737,0.0447
A,55,60.361,20.103,27.840,1.315,30.350,2.5,0.0737,0.0371
A,56,60.232,8.758,28.782,2.574,33.080,4.3,0.1073,0.0213
A,57,59.876,19.957,27.636,-0.551,29.427,1.8,0.0740,0.0479
A,58,59.785,19.105,25.815,0.387,27.663,1.8,0.0769,0.0479
A,59,59.296,21.084,26.266,1.749,28.823,2.6,0.0539,0.0326
A,60,59.267,18.429,29.140,4.291,34.831,5.7,0.0757,0.0088
A,61,59.223,19.987,27.408,0.076,29.483,2.1,0.0733,0.0422
A,62,59.217,21.200,26.299,0.593,29.275,3.0,0.0528,0.0237
A,63,59.215,21.092,26.245,1.620,28.925,2.7,0.0535,0.0299
A,64,59.111,18.773,25.730,0.254,27.869,2.1,0.0779,0.0428
A,65,59.108,18.505,28.721,4.533,31.149,2.4,0.0805,0.0405
A,66,58.477,9.992,27.910,-6.563,36.799,8.9,0.1034,0.0018
A,67,58.413,9.999,27.823,4.563,34.833,7.0,0.1033,0.0066
A,68,58.107,18.819,27.728,3.807,30.136,2.4,0.0754,0.0364
A,69,58.055,18.190,26.378,6.798,27.854,1.5,0.0800,0.0529
A,70,58.016,9.336,31.733,5.955,36.748,5.0,0.1012,0.0159
A,71,57.887,15.442,29.184,-0.697,36.794,7.6,0.0892,0.0002
A,72,57.706,9.794,31.696,6.058,37.166,5.5,0.1000,0.0128
A,73,57.660,18.007,27.490,1.508,29.776,2.3,0.0815,0.0406
A,74,57.570,18.001,25.855,4.793,27.956,2.1,0.0792,0.0421
A,75,57.451,18.003,27.452,6.468,30.075,2.6,0.0787,0.0325
A,76,56.674,10.196,27.786,2.975,34.865,7.1,0.1001,0.0036
A,77,55.757,16.945,26.007,7.326,27.212,1.2,0.0824,0.0546
A,78,55.710,12.643,32.456,6.572,36.014,3.6,0.0923,0.0189
A,79,54.518,16.059,26.160,3.892,27.472,1.3,0.0858,0.0506
A,80,53.929,6.332,27.300,5.697,34.228,6.9,0.0998,0.0031
A,81,53.082,11.338,27.856,-2.317,34.853,7.0,0.0928,0.0011
A,82,52.798,14.862,25.913,6.259,26.927,1.0,0.0900,0.0510
B,01,69.727,25.039,26.912,-0.589,30.315,3.4,0.1194,0.0613
B,02,68.851,25.102,28.058,3.677,34.233,6.2,0.1280,0.0354
B,03,68.838,23.993,27.040,0.285,30.355,3.3,0.1214,0.0621
B,04,68.758,25.060,27.532,3.538,34.335,6.8,0.1273,0.0279
B,05,68.313,26.036,26.471,2.229,30.259,3.8,0.1058,0.0417
B,06,68.249,24.134,26.623,-1.748,29.660,3.0,0.1196,0.0713
B,07,68.200,26.085,25.916,-0.603,29.328,3.4,0.1066,0.0598
B,08,68.182,26.026,29.495,1.215,33.014,3.5,0.1096,0.0445
B,09,68.143,26.177,29.169,1.159,32.546,3.4,0.1089,0.0543
B,10,68.032,25.889,25.682,2.367,29.309,3.6,0.1062,0.0481
B,11,67.309,25.072,28.929,0.996,32.080,3.2,0.1093,0.0578
B,12,67.279,25.872,28.618,1.066,31.753,3.1,0.1071,0.0638
B,13,67.041,22.999,27.790,2.292,31.222,3.4,0.1267,0.0506
B,14,66.949,24.784,29.227,1.214,32.515,3.3,0.1087,0.0456
B,15,66.559,24.166,28.766,8.880,36.710,7.9,0.1281,0.0086
B,16,66.378,25.130,25.716,-0.101,29.002,3.3,0.1052,0.0544
B,17,66.175,22.856,26.101,-0.147,28.837,2.7,0.1269,0.0770
B,18,66.126,21.107,27.443,5.612,32.292,4.8,0.1616,0.0396
B,19,66.081,23.115,26.946,-0.072,29.929,3.0,0.1256,0.0668
B,20,66.079,24.933,28.114,0.939,31.098,3.0,0.1073,0.0635
B,21,66.024,25.027,25.865,0.647,29.386,3.5,0.1049,0.0406
B,22,65.978,24.955,26.286,1.553,29.395,3.1,0.1044,0.0598
B,23,65.646,21.579,27.556,2.150,32.222,4.7,0.1543,0.0430
B,24,65.537,23.281,27.461,1.601,32.204,4.7,0.1352,0.0449
B,25,65.465,24.294,29.670,3.612,32.494,2.8,0.1066,0.0627
B,26,65.430,24.084,26.364,2.725,29.783,3.4,0.1069,0.0380
B,27,65.210,23.030,27.231,5.098,32.289,5.1,0.1315,0.0407
B,28,65.171,23.128,27.820,4.040,34.586,6.8,0.1340,0.0219
B,29,65.166,22.919,27.543,1.619,32.058,4.5,0.1367,0.0462
B,30,64.961,24.141,25.391,0.248,28.390,3.0,0.1050,0.0586
B,31,64.912,24.079,29.411,2.670,32.358,2.9,0.1056,0.0538
B,32,64.630,24.035,25.330,-0.845,28.164,2.8,0.1038,0.0657
B,33,64.570,22.841,28.346,2.006,30.958,2.6,0.1133,0.0721
B,34,64.374,24.084,29.890,4.425,32.969,3.1,0.1038,0.0429
B,35,64.057,21.881,28.041,3.994,34.635,6.6,0.1434,0.0233
B,36,64.028,21.905,28.377,10.141,36.593,8.2,0.1443,0.0057
B,37,63.935,21.659,27.617,2.098,33.001,5.4,0.1449,0.0359
B,38,63.271,23.255,26.009,-1.585,29.087,3.1,0.1037,0.0418
B,39,63.238,23.288,25.845,-1.400,28.726,2.9,0.1031,0.0529
B,40,63.199,23.130,25.831,-0.658,28.567,2.7,0.1045,0.0592
B,41,61.811,22.336,25.811,-1.594,28.441,2.6,0.1031,0.0596
B,42,61.747,22.322,25.844,-1.828,28.610,2.8,0.1023,0.0536
B,43,61.127,22.133,25.856,-0.165,28.758,2.9,0.1040,0.0427
B,44,60.088,21.109,25.328,1.210,27.768,2.4,0.1066,0.0622
B,45,59.816,20.143,25.833,-1.936,28.549,2.7,0.1144,0.0526
B,46,59.806,21.156,25.963,0.259,28.605,2.6,0.1068,0.0513
B,47,59.707,20.920,25.699,0.663,28.227,2.5,0.1078,0.0573
B,48,59.672,20.092,24.789,2.170,27.161,2.4,0.1139,0.0674
B,49,59.415,20.066,25.669,-1.625,28.158,2.5,0.1115,0.0603
B,50,58.151,19.050,26.054,-0.282,28.682,2.6,0.1212,0.0507
B,51,58.078,18.927,25.907,0.871,28.243,2.3,0.1219,0.0642
B,52,57.074,19.127,25.054,1.753,27.632,2.6,0.0819,0.0536
B,53,56.886,19.249,25.533,0.153,27.867,2.3,0.1079,0.0576
B,54,55.893,18.030,25.791,4.106,27.959,2.2,0.1329,0.0655
B,55,55.709,17.324,25.896,3.541,28.049,2.2,0.1439,0.0690
B,56,55.236,15.509,28.257,8.799,34.882,6.6,0.1692,0.0042
B,57,54.329,14.533,28.083,13.152,34.804,6.7,0.1750,0.0009
B,58,54.302,14.320,28.210,13.340,34.440,6.2,0.1749,0.0067
B,59,53.254,16.880,25.587,1.031,27.922,2.3,0.1060,0.0401
B,60,51.724,13.152,28.094,9.831,34.325,6.2,0.1743,0.0000
B,61,51.624,15.598,26.254,0.215,28.438,2.2,0.1123,0.0397
B,62,51.285,13.207,28.029,11.152,33.887,5.9,0.1723,0.0021
B,63,50.985,14.015,25.524,3.339,27.185,1.7,0.1614,0.0692
B,64,50.898,14.975,24.919,0.796,26.736,1.8,0.1204,0.0580
B,65,48.218,9.824,29.802,9.344,33.862,4.1,0.1691,0.0131
B,66,47.818,9.793,27.926,8.493,31.890,4.0,0.1716,0.0147
B,67,47.496,8.403,28.298,11.429,32.847,4.5,0.1722,0.0093
B,68,47.384,8.231,28.255,14.286,33.334,5.1,0.1721,0.0036
```

## Addendum (2026-11-24): documents added to `docs/ejector` by the user, and what became of the candidates

- **Liu & Groll 2008**, IRACC paper 2131 (*Analysis of a two phase flow ejector for transcritical CO2 cycle*): one
  complete operating point (Table 1: 12.855 MPa, 50.88 °C motive; 3.748 MPa suction; 0.18 / 0.07 kg/s; 4.499 MPa
  outlet) but no throat or mixer diameter, and the 29 runs only as back-fitted efficiencies (Fig. 9). Not usable
  without the ARTI-10110-01 report.
- **Huang et al. 1999** (original): the 39 points used in `validate_huang1999.py` agree with Tables 3–4.
- **GEA catalogue BTC0011-EN**: five charts digitised (`prototype/data/gea_*.csv`); design-mode benchmark.
- **Fabri & Paulon 1958** (NACA TM 1410): air-to-air, constant-area mixer, figures only; candidate for digitising.
- **Liu 2014** (efficiency review), **Kanbur et al. 2025**, **Lawrence & Elbel 2014**, **Chao et al. 2025**:
  reference only (no tabulated data with complete geometry; Chao 2025 has 4 points with internal inconsistencies).
- Used in the techref parameter guide: Bodys 2020 (motive flow), Falat 2018, Shestopalov 2015, Lawrence 2012 (not
  reproduced), GEA. Little 2015 transcribed (`data/little2015.csv`) but not usable: suction flow and outlet pressure
  are not tabulated per point.
