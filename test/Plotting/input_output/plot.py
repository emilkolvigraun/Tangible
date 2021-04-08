import pandas as pd


df = pd.read_csv('receiver_log0.txt').sort_values(by='val')[:-30]

indexes = range(1, len(df))

columns = list(range(0, 2001, 10))
# columns[0] = 1
print(columns)
# exit(0)
ndf = dict()
for c in columns:
    ndf.update({c:[]})

consumption_frequency = []
for i in indexes:
    t0 = int(df['T0'].iloc[i-1])
    t1 = int(df['T0'].iloc[i])
    # if (i<1000):
    #     key = list(str(int(((i+1)/1000)*10)))
    # else: 
    key = list(str(int(((i/1000))*10)))
    key[-1] = "0"
    key = int("".join(key))
    occurance_T = t1-t0
    period = 1000
    hertz = period / occurance_T
    # print(hertz)
    ndf[key].append(hertz)
    # consumption_frequency.append(hertz)

for k in columns:
    print(len(ndf[k]))

# exit(0)
import matplotlib.pyplot as plt

ax, fig = plt.subplots(figsize=(12,6))
# plt.scatter(indexes, consumption_frequency, alpha=.5)
fig.boxplot(ndf.values(), showmeans=True)
plt.grid(True)
plt.show()


# print(str(length), df.head())