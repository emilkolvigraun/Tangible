import pandas as pd


df = pd.read_csv('receiver_log3.txt').sort_values(by='val')[:-30]
indexes = range(1, len(df))


consumption_frequency = []
for i in indexes:
    at_index_i_1 = df.iloc[i-1]
    at_index_i = df.iloc[i]
    if (int(at_index_i['val']) <= int(at_index_i_1['val'])): print("FUCK")

    t0 = int(at_index_i_1['T0'])
    t1 = int(at_index_i['T0'])
    
    if ((t1-t0)==1): print('hello')
    occurance_T = 1000/(t1-t0)

    consumption_frequency.append(occurance_T)


import matplotlib.pyplot as plt

# plt.boxplot(consumption_frequency)
plt.scatter(indexes, consumption_frequency, alpha=.5)
plt.grid(True)
plt.show()