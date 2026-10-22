import type { WovenConstant, WovenDraft, WovenMaterial, WovenRate, WovenResult } from "@/lib/woven-quote-api";

export function blankWovenDraft(): WovenDraft {
  const today = new Date().toISOString().slice(0, 10);
  return {
    productType: "WovenBag",
    quoteDate: today,
    sector: "Export",
    customerName: "",
    ourRef: "",
    customerRef: "",
    bagType: "Tubular Bag",
    fabricColour: "MW",
    faceWidthCm: null,
    gussetCm: 0,
    lengthCm: null,
    top: "Hemmed",
    totalWeightGram: null,
    fabricGsm: null,
    linerMicron: null,
    glossyBoppMicron: null,
    mattBoppMicron: null,
    metalisedBoppMicron: null,
    whitePaperGsm: null,
    brownPaperGsm: null,
    adhesiveGsm: null,
    inkGsm: null,
    coatingGsm: null,
    handleGram: null,
    fabricRecipe: { PP: 92, FILLER: 5, COL: 3 },
    coatingRecipe: { LDPE7: 20, PP_H350: 80 },
    linerRecipe: { LLDPE: 90, LDPE_FY: 10 },
    fabricConversion: 25,
    coatingConversion: 14,
    linerConversion: 14,
    wastageFabric: 5,
    wastageLiner: 2,
    wastageFinishing: 3,
    realisationFabric: 50,
    realisationLiner: 40,
    bagFinishing: 1.4,
    thumbNotch: false,
    metalDetection: false,
    perforation: false,
    flexoPrinting: 0.6,
    rewindingPerKg: 0,
    packingPerKg: 1,
    palletPerKg: 4,
    fobCostPerKg: 3.5,
    paymentDays: 30,
    profitPercent: 8,
    usdInr: 88,
    euroPerUsd: 1.05,
    gbpPerUsd: 1.23,
    freightUsdPerKg: 0,
    commissionUsdPerKg: 0,
    dimensionUnit: "CM",
    terms: "FOB",
    seaPort: "Mundra",
    currency: "USD",
    packing: "Floor Loaded",
    pcsPerBale: null,
    palletSize: "",
    bagsPerPallet: null,
    printPlateCost: null,
    printPlateCostText: "",
    displayGsm: "",
    meterPerRoll: null,
    rollsPerPallet: null,
    remarks: "",
    adWidth: null,
    adLength: null,
    adPatch: null,
    adValveLength: null,
    adExtendedValve: null,
    adFabricGsm: null,
    adCoatingGsm: null,
    adBoppGsm: null,
    adOpenMouth: false,
    adUv: false,
    adPrinting: "",
    adLinerMicron: null,
    adConversion: null,
    adSeaFreight: 0,
    adPcsPerBale: null,
    adPcsPerPallet: null,
  };
}

export function ratesAsOf(rates: WovenRate[], sector: string, quoteDate: string): Record<string, number> {
  const picked = new Map<string, WovenRate>();
  for (const rate of rates) {
    if (rate.sector !== sector) continue;
    const day = rate.effectiveDate.slice(0, 10);
    const asOf = (quoteDate || "").slice(0, 10);
    if (!asOf || day > asOf) continue;
    const current = picked.get(rate.materialCode);
    if (!current || current.effectiveDate.slice(0, 10) < day) picked.set(rate.materialCode, rate);
  }
  return Object.fromEntries([...picked.entries()].map(([code, rate]) => [code, rate.rate]));
}

export function calculateWovenQuote(
  draft: WovenDraft,
  rateMap: Record<string, number>,
  constants: WovenConstant[],
  materials: WovenMaterial[],
): WovenResult {
  const c = Object.fromEntries(constants.map((row) => [row.key, row.value]));
  return draft.productType === "AdStar"
    ? adStar(draft, rateMap, c)
    : woven(draft, rateMap, c, materials);
}

function k(c: Record<string, number>, key: string, fallback: number) {
  return c[key] ?? fallback;
}
function z(value: number | null | undefined, fallback = 0) {
  return value == null || Number.isNaN(value) ? fallback : value;
}
function rate(rates: Record<string, number>, code: string) {
  return rates[code] ?? 0;
}
function area(width: number, length: number) {
  return (width * 2 * length) / 10000;
}
/** 5 means 5%. Values of 0.2 or less are older workbook fractions such as 0.05. */
export function wastageFraction(value: number | null | undefined, fallbackPercent: number) {
  if (value == null || Number.isNaN(value)) return fallbackPercent / 100;
  if (value > 0 && value <= 0.2) return value;
  return value / 100;
}

export function showWastagePercent(value: number | null): number | null {
  if (value == null) return null;
  if (value > 0 && value <= 0.2) return Math.round(value * 10000) / 100;
  return value;
}

function blend(recipe: Record<string, number>, rates: Record<string, number>, codes: string[]) {
  let sum = 0;
  let weighted = 0;
  for (const code of codes) {
    const pct = recipe[code] ?? 0;
    sum += pct;
    weighted += pct * rate(rates, code);
  }
  return sum === 0 ? 0 : weighted / sum;
}

function woven(
  d: WovenDraft,
  rates: Record<string, number>,
  c: Record<string, number>,
  materials: WovenMaterial[],
): WovenResult {
  const warnings: string[] = [];
  if (!d.quoteDate) warnings.push("Enter a quote date so raw-material rates can be picked.");
  if (!d.customerName.trim()) warnings.push("Enter a customer.");
  const density = k(c, "density", 0.92);
  const width = z(d.faceWidthCm);
  const gusset = z(d.gussetCm);
  const length = z(d.lengthCm);
  const top = d.top.trim();
  const bag = d.bagType.trim();
  const cutExtra = top === "Heat Cut" ? k(c, "cutHeat", 5) : top === "Hemmed" ? k(c, "cutHem", 8) : top === "Ez Open" ? k(c, "cutEz", 0) : top === "No Fold" ? k(c, "cutNoFold", 0) : 0;
  if (!top) warnings.push("Select the bag top. Cut length stays at the entered length until you do.");
  const cut = length + cutExtra;
  const backSeam = bag.toLowerCase() === "back seam bag";
  const gross = width + gusset + (backSeam ? k(c, "backSeamExtraCm", 2.5) : 0);
  const handle = z(d.handleGram);
  const easy = top === "Ez Open" ? (k(c, "easyOpenGramPerMeter", 7) / 100) * (width + k(c, "easyOpenWidthExtra", 10)) : 0;
  const liner = area(width + gusset + k(c, "linerWidthExtra", 2), cut + k(c, "linerLengthExtra", 5)) * z(d.linerMicron) * density;
  const boppWidth = width + gusset + k(c, "boppWidthExtra", 1.5);
  const glossy = area(boppWidth, cut) * z(d.glossyBoppMicron) * density;
  const matt = area(boppWidth, cut) * z(d.mattBoppMicron) * density;
  const metal = area(boppWidth, cut) * z(d.metalisedBoppMicron) * density;
  const white = area(boppWidth, cut) * z(d.whitePaperGsm);
  const brown = area(boppWidth, cut) * z(d.brownPaperGsm);
  const adhesive = area(boppWidth, cut) * z(d.adhesiveGsm) * density;
  const ink = area(boppWidth, cut) * z(d.inkGsm);
  const solvent = ink === 0 ? 0 : ink * k(c, "solventInkFactor", 2);
  const backSeamGram = backSeam ? (k(c, "backSeamCoatingGrPerM", 2) * cut) / 100 : 0;
  const coating = area(width + gusset + k(c, "coatingWidthExtra", 2), cut) * z(d.coatingGsm);
  const stitched = bag.toLowerCase() === "tubular bag" || backSeam;
  const divisor = k(c, "threadDivisor", 61) || 61;
  const thread = !stitched ? 0 : top === "Hemmed" ? (k(c, "threadHem", 3) / divisor) * width : top === "Heat Cut" ? (k(c, "threadHeat", 2) / divisor) * width : top === "Ez Open" ? (k(c, "threadEz", 1.2) / divisor) * (width + k(c, "threadEzWidthExtra", 10)) : 0;
  const parts = handle + easy + liner + glossy + matt + metal + white + brown + adhesive + ink + backSeamGram + coating + thread;
  const gsmBlank = d.fabricGsm == null;
  const fabricFromGsm = gsmBlank ? 0 : area(gross, cut) * z(d.fabricGsm);
  const calculated = gsmBlank ? 0 : fabricFromGsm + parts;
  let fabric: number;
  let sellingWeight: number;
  if (d.totalWeightGram != null && d.totalWeightGram > calculated) {
    sellingWeight = d.totalWeightGram;
    fabric = d.totalWeightGram - parts;
  } else {
    if (d.totalWeightGram != null && d.totalWeightGram < calculated) warnings.push("Entered weight is below the calculated weight, so the calculated weight is used.");
    sellingWeight = calculated;
    fabric = fabricFromGsm;
  }
  const fabricGsm = gross <= 0 || cut <= 0 ? 0 : Math.round((fabric / ((gross * 2 * cut) / 10000)) * 100) / 100;
  const fabricRmc = blend(d.fabricRecipe, rates, materials.filter((m) => m.inFabric).map((m) => m.code));
  const coatingRmc = blend(d.coatingRecipe, rates, materials.filter((m) => m.inCoating).map((m) => m.code));
  const linerRmc = blend(d.linerRecipe, rates, materials.filter((m) => m.inLiner).map((m) => m.code));
  for (const [name, recipe] of [["Fabric", d.fabricRecipe], ["Coating", d.coatingRecipe], ["Liner", d.linerRecipe]] as const) {
    const sum = Object.values(recipe).reduce((a, b) => a + (b || 0), 0);
    if (sum > 0 && Math.abs(sum - 100) > 0.05) warnings.push(`${name} recipe adds up to ${sum.toFixed(2)}%. The blend still uses that total, the same way the workbook does.`);
  }
  const printDen = white + brown + glossy + matt + metal + adhesive + ink;
  const printNum = white * rate(rates, "WHITE_PAPER") + brown * rate(rates, "BROWN_PAPER") + glossy * rate(rates, "GLOSSY") + matt * rate(rates, "MATT") + metal * rate(rates, "METAL") + adhesive * rate(rates, "ADHESIVE") + ink * rate(rates, "INK") + solvent * rate(rates, "SOLVENT");
  const printWaste = z(d.inkGsm) > 0 ? k(c, "printWasteFraction", 0.05) : 0;
  const printedRmc = printDen === 0 ? 0 : (printNum / printDen) * (1 + printWaste);
  const ez = handle + easy + thread;
  const totalGram = fabric + coating + liner + backSeamGram + printDen + ez;
  const uvRate = rate(rates, "UV");
  const backRate = rate(rates, "BACKSEAM");
  const totalRmc = totalGram === 0 ? 0 : (fabric * fabricRmc + coating * coatingRmc + liner * linerRmc + backSeamGram * backRate + printDen * printedRmc + ez * uvRate) / totalGram;
  const convF = z(d.fabricConversion, 25);
  const convC = z(d.coatingConversion, 14);
  const convL = z(d.linerConversion, 14);
  const paperGsm = z(d.whitePaperGsm) + z(d.brownPaperGsm);
  const boppMic = z(d.glossyBoppMicron) + z(d.mattBoppMicron) + z(d.metalisedBoppMicron);
  const convP = z(d.inkGsm) <= 0 ? 0 : paperGsm > 0 ? k(c, "printConvPaper", 10) : boppMic > 0 ? k(c, "printConvBopp", 40) : 0;
  const wasF = wastageFraction(d.wastageFabric, 5);
  const wasL = wastageFraction(d.wastageLiner, 2);
  const wasFin = wastageFraction(d.wastageFinishing, 3);
  const span = (width + gusset) * 2;
  const wasC = z(d.coatingGsm) > 0 && span > 0 ? ((bag.toLowerCase() === "tubular bag" || bag.toLowerCase() === "tubular fabric" ? k(c, "coatWasteTubularNum", 1.75) : k(c, "coatWasteOtherNum", 3.75)) / span) + k(c, "coatWasteBase", 0.0075) : 0;
  const realF = z(d.realisationFabric, 50);
  const realL = z(d.realisationLiner, 40);
  const realC = paperGsm > 0 ? k(c, "realisationCoatingPaper", 0) : k(c, "realisationCoatingNoPaper", 35);
  const realFin = paperGsm > 0 ? k(c, "realisationFinishingPaper", 8) : k(c, "realisationFinishingNoPaper", 35);
  const thumb = d.thumbNotch ? k(c, "thumbNotch", 0.4) : 0;
  const metalCost = d.metalDetection ? k(c, "metalDetection", 0.3) : 0;
  const perf = d.perforation ? k(c, "perforation", 0.2) : 0;
  const flexo = z(d.flexoPrinting, 0.6);
  const finishSum = z(d.bagFinishing, 1.4) + thumb + metalCost + perf + flexo;
  const coatWeight = k(c, "coatingWastageUsesTotalWeight", 1) >= 1 ? totalGram : coating;
  let waste = ((fabricRmc + convF - realF) * wasF * fabric) / 1000 + (d.coatingGsm == null ? 0 : ((coatingRmc + convC - realC) * coatWeight * wasC) / 1000) + ((linerRmc + convL - realL) * wasL * liner) / 1000;
  if (totalGram > 0) {
    const convBag = (fabric * convF + coating * convC + liner * convL + printDen * convP) / 1000;
    waste += ((((totalRmc + ((convBag + finishSum) / totalGram) * 1000) - realFin) * wasFin * totalGram) / 1000);
  }
  const perKg = z(d.rewindingPerKg) + z(d.packingPerKg, 1) + z(d.palletPerKg, 4) + z(d.fobCostPerKg, 3.5);
  const costPc = ((fabricRmc + convF) * fabric) / 1000 + ((coatingRmc + convC) * coating) / 1000 + ((linerRmc + convL) * liner) / 1000 + (backRate * backSeamGram) / 1000 + ((printedRmc + convP) * printDen) / 1000 + (ez * uvRate) / 1000 + waste + finishSum + (perKg * totalGram) / 1000;
  const costKg = totalGram === 0 ? 0 : (costPc / totalGram) * 1000;
  const year = k(c, "yearDays", 365) || 365;
  const wc = (costKg * (k(c, "wcBaseDays", 30) + z(d.paymentDays, 30))) / year * k(c, "wcRate", 0.125);
  const fixedAsset = k(c, "fixedAssetPerKg", 13.14);
  const profit = ((costKg + wc + fixedAsset) * z(d.profitPercent, 8)) / 100;
  const quote = costKg + wc + fixedAsset + profit;
  const usd = z(d.usdInr, 88);
  const euro = z(d.euroPerUsd, 1.05);
  const gbp = z(d.gbpPerUsd, 1.23);
  const commission = z(d.commissionUsdPerKg);
  const freight = z(d.freightUsdPerKg);
  if (usd === 0) warnings.push("USD / INR is zero, so the foreign-currency price is blank.");
  const fobUsd = usd === 0 ? 0 : quote / usd + commission;
  const again = k(c, "cifAddsCommissionAgain", 1) >= 1;
  const cifUsd = fobUsd + freight + (again ? commission : 0);
  const delivered = (fobUsd + freight + (again ? commission : 0)) * usd;
  const currency = d.currency.trim();
  const terms = d.terms.trim();
  const fobLike = terms.toLowerCase() === "fob";
  const exWorks = terms.toLowerCase() === "ex works";
  const perKgPrice = currency === "USD" ? (fobLike ? fobUsd : cifUsd) : currency === "Euro" ? (fobLike ? (euro === 0 ? 0 : fobUsd / euro) : euro === 0 ? 0 : cifUsd / euro) : currency === "GBP" ? (fobLike ? (gbp === 0 ? 0 : fobUsd / gbp) : gbp === 0 ? 0 : cifUsd / gbp) : currency === "INR" ? (exWorks ? quote : delivered) : 0;
  if (!currency) warnings.push("Select a currency.");
  const flat = bag.toLowerCase() === "flat fabric";
  const inch = d.dimensionUnit.toLowerCase() === "inch";
  const offerWidth = fabric <= 0 ? null : inch ? (flat ? (width / 2.54) * 2 : width / 2.54) : flat ? width * 2 : width;
  const offerGusset = fabric <= 0 ? null : inch ? gusset / 2.54 : gusset;
  const offerLength = fabric <= 0 ? null : inch ? length / 2.54 : length;
  const printed = z(d.inkGsm) > 0 && z(d.glossyBoppMicron) > 0 ? "Printed Glossy BOPP" : z(d.inkGsm) > 0 && z(d.mattBoppMicron) > 0 ? "Printed Matte BOPP" : z(d.inkGsm) > 0 && z(d.metalisedBoppMicron) > 0 ? "Printed Metalised BOPP" : z(d.inkGsm) > 0 && paperGsm > 0 ? "Printed Paper" : flexo > 0 ? "Yes" : "No";
  const line = (group: string, label: string, value: number, unit: string) => ({ group, label, value, unit });
  return {
    warnings,
    cutLengthCm: cut,
    grossWidthCm: gross,
    fabricGram: fabric,
    fabricGsmCalculated: fabricGsm,
    calculatedWeightGram: calculated,
    totalComponentGram: totalGram,
    fabricRmc,
    coatingRmc,
    linerRmc,
    printedRmc,
    totalRmcPerKg: totalRmc,
    wastageCost: waste,
    costPerPiece: costPc,
    costPerKg: costKg,
    workingCapitalPerKg: wc,
    profitPerKg: profit,
    quotePerKg: quote,
    fobUsdPerKg: fobUsd,
    cifUsdPerKg: cifUsd,
    deliveredInrPerKg: delivered,
    pricePerPiece: perKgPrice * sellingWeight / 1000,
    priceBasis: `${terms} ${currency}`.trim(),
    printedLabel: printed,
    coatedLabel: coating > 0 ? "Yes" : "No",
    uvLabel: (d.fabricRecipe.UV ?? 0) > 0 ? "Yes" : "No",
    linerLabel: z(d.linerMicron) > 0 ? "Yes" : "No",
    offerWidth,
    offerGusset,
    offerLength,
    lines: [
      line("Weight", "Cut length", cut, "cm"),
      line("Weight", "Gross width", gross, "cm"),
      line("Weight", "Fabric", fabric, "g"),
      line("Weight", "Fabric GSM", fabricGsm, "gsm"),
      line("Weight", "Liner", liner, "g"),
      line("Weight", "Coating", coating, "g"),
      line("Weight", "Printed film and paper", printDen, "g"),
      line("Weight", "Thread", thread, "g"),
      line("Weight", "Handle and easy open", handle + easy, "g"),
      line("Weight", "Check weight from entered GSM", calculated, "g"),
      line("Material", "Fabric raw material", fabricRmc, "Rs/kg"),
      line("Material", "Coating raw material", coatingRmc, "Rs/kg"),
      line("Material", "Liner raw material", linerRmc, "Rs/kg"),
      line("Material", "Printed film / paper", printedRmc, "Rs/kg"),
      line("Material", "Blended raw material", totalRmc, "Rs/kg"),
      line("Cost", "Wastage", waste, "Rs/pc"),
      line("Cost", "Manufacturing cost", costPc, "Rs/pc"),
      line("Cost", "Manufacturing cost", costKg, "Rs/kg"),
      line("Cost", "Working capital", wc, "Rs/kg"),
      line("Cost", "Profit", profit, "Rs/kg"),
      line("Price", "Quote", quote, "Rs/kg"),
      line("Price", "FOB", fobUsd, "USD/kg"),
      line("Price", "CIF", cifUsd, "USD/kg"),
      line("Wastage", "Coating wastage", wasC * 100, "%"),
      line("Wastage", "Coating realisation", realC, "Rs/kg"),
      line("Wastage", "Finishing realisation", realFin, "Rs/kg"),
      line("Price", "Quotation", perKgPrice * sellingWeight / 1000, currency ? `${currency}/pc` : "per pc"),
    ],
  };
}

function adStar(d: WovenDraft, rates: Record<string, number>, c: Record<string, number>): WovenResult {
  const warnings: string[] = [];
  if (!d.quoteDate) warnings.push("Enter a quote date so the PP rate can be picked.");
  const width = z(d.adWidth);
  const length = z(d.adLength);
  const patch = z(d.adPatch);
  const gsm = z(d.adFabricGsm) + z(d.adCoatingGsm) + z(d.adBoppGsm);
  const extra = k(c, "adPatchGsmExtra", 20);
  const density = k(c, "density", 0.92);
  const bag = d.adOpenMouth
    ? (width * 2 * (length + patch / 2 + k(c, "adOpenLengthExtra", 1.5))) / 10000 * gsm + ((gsm + extra) * width * patch) / 10000
    : (width * 2 * (length + patch + k(c, "adClosedLengthExtra", 3))) / 10000 * gsm + (width * patch * 2 * (gsm + extra)) / 10000 + (patch * (z(d.adValveLength) + z(d.adExtendedValve)) * 2) / 10000 * (z(d.adFabricGsm) + z(d.adCoatingGsm) + z(d.adCoatingGsm));
  const liner = ((width + k(c, "adLinerWidthExtra", 3)) * 2 * (length + patch) * z(d.adLinerMicron) * density) / 10000;
  const total = bag + liner;
  const pp = rate(rates, "PP");
  const fobInr = pp + z(d.adConversion);
  const usd = z(d.usdInr, 88);
  const euro = z(d.euroPerUsd, 1.08);
  const freight = z(d.adSeaFreight);
  const usdPc = usd === 0 ? 0 : ((fobInr + freight) / usd) * total / 1000;
  const line = (group: string, label: string, value: number, unit: string) => ({ group, label, value, unit });
  return {
    warnings,
    cutLengthCm: 0,
    grossWidthCm: width,
    fabricGram: bag,
    fabricGsmCalculated: z(d.adFabricGsm),
    calculatedWeightGram: total,
    totalComponentGram: total,
    fabricRmc: pp,
    coatingRmc: 0,
    linerRmc: 0,
    printedRmc: 0,
    totalRmcPerKg: pp,
    wastageCost: 0,
    costPerPiece: usdPc,
    costPerKg: fobInr,
    workingCapitalPerKg: 0,
    profitPerKg: 0,
    quotePerKg: fobInr + freight,
    fobUsdPerKg: total === 0 || usd === 0 ? 0 : (usdPc / total) * 1000,
    cifUsdPerKg: 0,
    deliveredInrPerKg: fobInr + freight,
    pricePerPiece: usdPc,
    priceBasis: "USD per piece",
    printedLabel: d.adPrinting || "—",
    coatedLabel: z(d.adCoatingGsm) > 0 ? "Yes" : "No",
    uvLabel: d.adUv ? "Yes" : "No",
    linerLabel: z(d.adLinerMicron) > 0 ? "Yes" : "No",
    offerWidth: width,
    offerGusset: null,
    offerLength: length,
    lines: [
      line("Weight", "Bag", bag, "g"),
      line("Weight", "Liner", liner, "g"),
      line("Weight", "Total", total, "g"),
      line("Material", "PP rafia", pp, "Rs/kg"),
      line("Price", "Quotation", usdPc, "USD/pc"),
      line("Price", "Quotation", euro === 0 ? 0 : usdPc / euro, "Euro/pc"),
    ],
  };
}

export function formatQty(value: number, digits = 2) {
  if (!Number.isFinite(value)) return "—";
  return value.toLocaleString("en-IN", { minimumFractionDigits: digits, maximumFractionDigits: digits });
}
