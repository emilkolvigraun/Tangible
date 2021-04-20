import pandas as pd
import matplotlib.pyplot as plt

y = 'total'
x = 'id'
length = 10000

df1 = pd.read_csv('3_1.csv')[:length]
sct1 = plt.scatter(df1[x], df1[y], label='1', zorder=10, alpha=.1)
plt.hlines(sum(df1[y])/len(df1), min(df1[x]), max(df1[x]), zorder=11, color='r')
sct1.set_rasterized(True)
print(len(df1))

df10 = pd.read_csv('3_10.csv')[:length]
sct10 = plt.scatter(df10[x], df10[y], label='10', zorder=9, alpha=.1)
plt.hlines(sum(df10[y])/len(df10), min(df10[x]), max(df10[x]), zorder=11, color='g')
sct10.set_rasterized(True)
print(len(df10))

df15 = pd.read_csv('3_15.csv')[:length]
sct15 = plt.scatter(df15[x], df15[y], label='15', zorder=8, alpha=.1)
plt.hlines(sum(df15[y])/len(df15), min(df15[x]), max(df15[x]), zorder=11, color='b')
sct15.set_rasterized(True)
print(len(df15))

df20 = pd.read_csv('3_20.csv')[:length]
sct20 = plt.scatter(df20[x], df20[y], label='20', zorder=7, alpha=.1)
plt.hlines(sum(df20[y])/len(df20), min(df20[x]), max(df20[x]), zorder=11, color='k')
sct20.set_rasterized(True)
print(len(df20))

plt.show()