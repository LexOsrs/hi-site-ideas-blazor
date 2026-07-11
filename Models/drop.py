import random

def get_drop():
    for i in range(1, 10_000):
        if random.randint(1, 400) == 400:
            return i
    
    return None

for _ in range(10):
    print(get_drop())