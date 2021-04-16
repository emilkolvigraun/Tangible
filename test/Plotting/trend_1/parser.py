import pandas as pd

df = pd.read_csv('3/TcpNode0.txt')[10:10000].sort_values(by='id').reset_index(drop=True)



dct = {'time':[], 'id':[], 'interval':[]}
col = 'send'

T0 = df.iloc[0][col]

print((df.iloc[-1][col]-T0)/1000/1000)
for i in range(1, len(df)):
    
    r0 = df.iloc[i-1]
    r1 = df.iloc[i]

    consumption_interval = r1[col] - r0[col]
    T1 = r1[col] - T0

    dct['time'].append(T1)
    dct['id'].append(r1.id)
    dct['interval'].append(consumption_interval/1000)


# print(df.head())

import matplotlib.pyplot as plt

plt.scatter(dct['id'], dct['interval'], alpha=.6)
plt.show()