import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv('4/TcpNode0.txt')

latencies = []
batches = []
for b in set(df.batch):
    batch = df[df.batch==b].sort_values(by='receive').reset_index(drop=True)
    print(batch.head())
    T0 = int(batch.iloc[0].receive)
    T1 = int(batch.iloc[-1].receive)
    batches.append(b)
    latencies.append(
        T1-T0
    )

plt.plot(batches, latencies)
plt.show()