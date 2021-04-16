import pandas as pd
import matplotlib.pyplot as plt








def get_within_second(fn, col):
    df = pd.read_csv(fn)
    batch = df.sort_values(by=col).reset_index(drop=True)
    secondly = {}
    start = batch.iloc[0][col]
    for i in batch.index:
        point = batch.iloc[i]
        within_second = int(((point[col]-start)/1000)/1000)
        if within_second not in secondly:
            secondly.update({within_second:0})
        secondly[within_second]+=1
    lists = sorted(secondly.items()) 
    x, y = zip(*lists)
    return x, y

x, y = get_within_second('4_2/TcpNode2.txt', 'send')
plt.plot(x, y, label="4")

x_1, y_1 = get_within_second('3/TcpNode4.txt', 'send')
plt.plot(x_1, y_1, label="3")

plt.show()