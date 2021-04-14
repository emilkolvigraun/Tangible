import pandas as pd
import matplotlib.pyplot as plt


colors = ['r', 'g', 'b']
j = 0
for i in [2, 3, 4]:
    df = pd.read_csv(str(i)+'_totals.csv')
    plt.scatter(df['batch'], df['completed'], marker='s', color=colors[j], label=str(i)+' nodes')
    plt.plot(df['batch'], df['completed'], color=colors[j])
    j+=1

plt.grid(True)
plt.xticks([1, 5, 10, 15, 20])
plt.legend()
plt.show()