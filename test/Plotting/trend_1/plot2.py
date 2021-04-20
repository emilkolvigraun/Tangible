import pandas as pd
import matplotlib.pyplot as plt


df6 = pd.read_csv('6_tt.csv')
df5 = pd.read_csv('5_tt.csv')
df4 = pd.read_csv('4_tt.csv')
df3 = pd.read_csv('3_tt.csv')


sca6 = plt.scatter(range(len(df6)), df6.delta, label='6', alpha=.1)
sca6.set_rasterized(True)
sca5 = plt.scatter(range(len(df5)), df5.delta, label='5', alpha=.1)
sca5.set_rasterized(True)
sca4 = plt.scatter(range(len(df4)), df4.delta, label='4', alpha=.1)
sca4.set_rasterized(True)
sca3 = plt.scatter(range(len(df3)), df3.delta, label='3', alpha=.1)
sca3.set_rasterized(True)
plt.legend()
plt.gca().ticklabel_format(useOffset=False)
plt.show()