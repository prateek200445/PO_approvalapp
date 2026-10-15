from decimal import Decimal as D

width = D(100)
gusset = D(0)
length = D(155)
total = D(273)
gsm = D(58)
liner_mic = D(20)
handle = D("15.6")
cut = length + 8
gross = width + gusset
liner = (width + gusset + 2) * 2 * (cut + 5) / D(10000) * liner_mic * D("0.92")
thread = D(3) / D(61) * width
fabric = total - (handle + liner + thread)
calc_w = gross * 2 * cut / D(10000) * gsm + handle + liner + thread
print("cut", cut)
print("liner", liner)
print("thread", thread)
print("fabric", fabric)
print("calc", calc_w)

pp, filler, col = D("133.274"), D(40), D(250)
ld7, h350 = D("173.632"), D("134.564")
ll, fy = D("141.084"), D("170.884")
uv = D(280)
fab_rmc = (D(92) * pp + D(5) * filler + D(3) * col) / D(100)
coat_rmc = (D(20) * ld7 + D(80) * h350) / D(100)
lin_rmc = (D(90) * ll + D(10) * fy) / D(100)
w_fab, w_lin, w_ez = fabric, liner, handle + thread
w_tot = w_fab + w_lin + w_ez
rmc = (w_fab * fab_rmc + w_lin * lin_rmc + w_ez * uv) / w_tot
conv_f, conv_l = D(25), D(14)
was_f, was_l, was_fin = D("0.05"), D("0.02"), D("0.03")
real_f, real_l, real_fin = D(50), D(40), D(35)
fin_sum = D("1.4") + D("0.6")
part1 = (fab_rmc + conv_f - real_f) * was_f * w_fab / D(1000)
part3 = (lin_rmc + conv_l - real_l) * w_lin * was_l / D(1000)
conv_bag = w_fab * conv_f / D(1000) + w_lin * conv_l / D(1000)
adj = ((rmc + (conv_bag + fin_sum) / w_tot * D(1000)) - real_fin) * was_fin * w_tot / D(1000)
wastage = part1 + part3 + adj
perkg = D(1) + D(4) + D("3.5")
cost_pc = (
    (fab_rmc + conv_f) * w_fab / D(1000)
    + (lin_rmc + conv_l) * w_lin / D(1000)
    + w_ez * uv / D(1000)
    + wastage
    + fin_sum
    + perkg * w_tot / D(1000)
)
cost_kg = cost_pc / w_tot * D(1000)
wc = cost_kg * (D(30) + D(30)) / D(365) * D("0.125")
fixed = D("13.14")
profit = (cost_kg + wc + fixed) * D(8) / D(100)
quote = cost_kg + wc + fixed + profit
fob = quote / D(88)
price = fob * total / D(1000)
print("wastage", wastage)
print("cost_pc", cost_pc)
print("cost_kg", cost_kg)
print("wc", wc)
print("profit", profit)
print("quote", quote)
print("fob", fob)
print("price", price)
