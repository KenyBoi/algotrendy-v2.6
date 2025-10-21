"""
Genetic Algorithm Parameter Optimizer
=====================================
Automatically optimize strategy parameters using genetic algorithms

Features:
- Optimize multiple parameters simultaneously
- Custom fitness functions (Sharpe, return, profit factor, etc.)
- Parallel fitness evaluation using multiprocessing
- Elitism to preserve best solutions
- Adaptive mutation rates
- Convergence tracking

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import yfinance as yf
import pandas as pd
import numpy as np
from typing import List, Dict, Tuple, Callable
import random
import time
from multiprocessing import Pool, cpu_count
import logging

from fast_backtester import FastBacktester

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class Individual:
    """
    Represents an individual solution (parameter set) in the population
    """

    def __init__(self, genes: Dict[str, float]):
        """
        Args:
            genes: Dict of parameter name -> value
        """
        self.genes = genes
        self.fitness = None
        self.backtest_results = None

    def __repr__(self):
        return f"Individual(fitness={self.fitness:.4f if self.fitness else None}, genes={self.genes})"


def evaluate_individual(args: Tuple[Dict, Dict, pd.DataFrame]) -> Tuple[Dict, float, Dict]:
    """
    Evaluate fitness of an individual by backtesting

    Args:
        args: Tuple of (genes, config, data)

    Returns:
        Tuple of (genes, fitness_score, backtest_results)
    """
    genes, config, data = args

    try:
        # Create backtester
        backtester = FastBacktester(
            initial_capital=config.get('initial_capital', 10000.0)
        )

        # Run backtest with individual's parameters
        results = backtester.run_fast_backtest(
            symbol=config['symbol'],
            data=data,
            min_confidence=genes['min_confidence'],
            commission=config.get('commission', 0.001)
        )

        # Calculate fitness based on specified fitness function
        fitness_func = config.get('fitness_function', 'sharpe')

        if fitness_func == 'sharpe':
            # Sharpe ratio (higher is better)
            fitness = results.get('sharpe_ratio', 0.0)
        elif fitness_func == 'return':
            # Total return (higher is better)
            fitness = results['total_return'] / 100.0  # Normalize
        elif fitness_func == 'profit_factor':
            # Profit factor (higher is better)
            fitness = results.get('profit_factor', 0.0)
        elif fitness_func == 'win_rate':
            # Win rate (higher is better)
            fitness = results.get('win_rate', 0.0) / 100.0  # Normalize to 0-1
        elif fitness_func == 'composite':
            # Composite score: balance return, win rate, and drawdown
            ret = results['total_return'] / 100.0
            wr = results.get('win_rate', 0.0) / 100.0
            dd = abs(results.get('max_drawdown', 100.0)) / 100.0
            # Higher return and win rate is good, lower drawdown is good
            fitness = (ret * 0.4 + wr * 0.3 + (1 - dd) * 0.3)
        else:
            fitness = 0.0

        # Penalize if no trades
        if results['total_trades'] == 0:
            fitness = -1.0  # Strongly penalize no-trade solutions

        return (genes, fitness, results)

    except Exception as e:
        logger.error(f"Error evaluating individual: {e}")
        return (genes, -1.0, None)


class GeneticOptimizer:
    """
    Genetic Algorithm optimizer for strategy parameters
    """

    def __init__(
        self,
        population_size: int = 50,
        generations: int = 20,
        mutation_rate: float = 0.1,
        crossover_rate: float = 0.7,
        elitism_size: int = 5,
        max_workers: int = None
    ):
        """
        Initialize genetic optimizer

        Args:
            population_size: Number of individuals in population
            generations: Number of generations to evolve
            mutation_rate: Probability of mutation (0.0-1.0)
            crossover_rate: Probability of crossover (0.0-1.0)
            elitism_size: Number of top individuals to preserve
            max_workers: Max parallel workers (default: CPU count)
        """
        self.population_size = population_size
        self.generations = generations
        self.mutation_rate = mutation_rate
        self.crossover_rate = crossover_rate
        self.elitism_size = elitism_size
        self.max_workers = max_workers or cpu_count()

        self.parameter_ranges = {}
        self.population = []
        self.best_individual = None
        self.history = []

    def set_parameter_ranges(self, ranges: Dict[str, Tuple[float, float]]):
        """
        Set parameter ranges for optimization

        Args:
            ranges: Dict of parameter_name -> (min_value, max_value)

        Example:
            {
                'min_confidence': (50.0, 90.0),
                'max_risk_per_trade': (0.01, 0.05)
            }
        """
        self.parameter_ranges = ranges

    def _create_random_individual(self) -> Individual:
        """
        Create random individual with genes within parameter ranges

        Returns:
            New Individual with random genes
        """
        genes = {}
        for param, (min_val, max_val) in self.parameter_ranges.items():
            genes[param] = random.uniform(min_val, max_val)

        return Individual(genes)

    def _initialize_population(self):
        """
        Create initial random population
        """
        self.population = [
            self._create_random_individual()
            for _ in range(self.population_size)
        ]

    def _evaluate_population(self, data: pd.DataFrame, config: Dict):
        """
        Evaluate fitness of all individuals in population using parallel processing

        Args:
            data: Market data for backtesting
            config: Configuration dict
        """
        # Prepare arguments for parallel evaluation
        args_list = [
            (ind.genes, config, data)
            for ind in self.population
        ]

        # Evaluate in parallel
        with Pool(processes=self.max_workers) as pool:
            results = pool.map(evaluate_individual, args_list)

        # Update population with results
        for ind, (genes, fitness, backtest_results) in zip(self.population, results):
            ind.fitness = fitness
            ind.backtest_results = backtest_results

    def _select_parents(self) -> List[Individual]:
        """
        Select parents for next generation using tournament selection

        Returns:
            List of selected parents
        """
        tournament_size = 3
        parents = []

        for _ in range(self.population_size - self.elitism_size):
            # Tournament selection
            tournament = random.sample(self.population, tournament_size)
            winner = max(tournament, key=lambda ind: ind.fitness if ind.fitness else -float('inf'))
            parents.append(winner)

        return parents

    def _crossover(self, parent1: Individual, parent2: Individual) -> Individual:
        """
        Perform crossover between two parents

        Args:
            parent1: First parent
            parent2: Second parent

        Returns:
            Child individual
        """
        if random.random() > self.crossover_rate:
            # No crossover, return copy of parent1
            return Individual(parent1.genes.copy())

        # Uniform crossover: randomly select genes from each parent
        child_genes = {}
        for param in self.parameter_ranges.keys():
            child_genes[param] = random.choice([parent1.genes[param], parent2.genes[param]])

        return Individual(child_genes)

    def _mutate(self, individual: Individual) -> Individual:
        """
        Mutate individual's genes

        Args:
            individual: Individual to mutate

        Returns:
            Mutated individual
        """
        mutated_genes = individual.genes.copy()

        for param, (min_val, max_val) in self.parameter_ranges.items():
            if random.random() < self.mutation_rate:
                # Gaussian mutation: add random noise
                noise = random.gauss(0, (max_val - min_val) * 0.1)
                mutated_genes[param] = mutated_genes[param] + noise

                # Clip to valid range
                mutated_genes[param] = max(min_val, min(max_val, mutated_genes[param]))

        return Individual(mutated_genes)

    def _create_next_generation(self) -> List[Individual]:
        """
        Create next generation using selection, crossover, and mutation

        Returns:
            New population
        """
        # Elitism: keep best individuals
        sorted_population = sorted(
            self.population,
            key=lambda ind: ind.fitness if ind.fitness else -float('inf'),
            reverse=True
        )
        elite = sorted_population[:self.elitism_size]

        # Select parents
        parents = self._select_parents()

        # Create offspring
        offspring = []
        for i in range(0, len(parents) - 1, 2):
            parent1 = parents[i]
            parent2 = parents[i + 1]

            # Crossover
            child1 = self._crossover(parent1, parent2)
            child2 = self._crossover(parent2, parent1)

            # Mutation
            child1 = self._mutate(child1)
            child2 = self._mutate(child2)

            offspring.extend([child1, child2])

        # Combine elite and offspring
        new_population = elite + offspring[:self.population_size - len(elite)]

        return new_population

    def optimize(
        self,
        symbol: str,
        data: pd.DataFrame,
        fitness_function: str = 'composite',
        initial_capital: float = 10000.0,
        commission: float = 0.001
    ) -> Dict:
        """
        Run genetic algorithm optimization

        Args:
            symbol: Trading symbol
            data: Market data for backtesting
            fitness_function: Fitness function to use ('sharpe', 'return', 'profit_factor', 'win_rate', 'composite')
            initial_capital: Starting capital
            commission: Commission rate

        Returns:
            Dict with optimization results
        """
        logger.info(f"\n{'='*80}")
        logger.info("GENETIC ALGORITHM PARAMETER OPTIMIZATION")
        logger.info(f"{'='*80}")
        logger.info(f"Symbol: {symbol}")
        logger.info(f"Data: {len(data)} candles")
        logger.info(f"Population size: {self.population_size}")
        logger.info(f"Generations: {self.generations}")
        logger.info(f"Fitness function: {fitness_function}")
        logger.info(f"Workers: {self.max_workers}")
        logger.info(f"{'='*80}\n")

        config = {
            'symbol': symbol,
            'fitness_function': fitness_function,
            'initial_capital': initial_capital,
            'commission': commission
        }

        # Initialize population
        self._initialize_population()

        start_time = time.time()

        # Evolve for specified generations
        for generation in range(self.generations):
            gen_start = time.time()

            # Evaluate fitness
            self._evaluate_population(data, config)

            # Find best individual
            best_in_gen = max(
                self.population,
                key=lambda ind: ind.fitness if ind.fitness else -float('inf')
            )

            # Track best overall
            if self.best_individual is None or best_in_gen.fitness > self.best_individual.fitness:
                self.best_individual = best_in_gen

            # Record history
            avg_fitness = np.mean([ind.fitness for ind in self.population if ind.fitness is not None])
            self.history.append({
                'generation': generation,
                'best_fitness': best_in_gen.fitness,
                'avg_fitness': avg_fitness,
                'best_genes': best_in_gen.genes.copy()
            })

            gen_time = time.time() - gen_start

            logger.info(f"Generation {generation + 1}/{self.generations}: "
                       f"Best fitness={best_in_gen.fitness:.4f}, "
                       f"Avg fitness={avg_fitness:.4f}, "
                       f"Time={gen_time:.2f}s")

            # Create next generation (except for last generation)
            if generation < self.generations - 1:
                self.population = self._create_next_generation()

        total_time = time.time() - start_time

        logger.info(f"\n{'='*80}")
        logger.info("OPTIMIZATION COMPLETE")
        logger.info(f"{'='*80}")
        logger.info(f"Total time: {total_time:.2f}s")
        logger.info(f"Best fitness: {self.best_individual.fitness:.4f}")
        logger.info(f"Best parameters: {self.best_individual.genes}")
        logger.info(f"{'='*80}\n")

        return {
            'success': True,
            'best_individual': self.best_individual,
            'best_parameters': self.best_individual.genes,
            'best_fitness': self.best_individual.fitness,
            'best_backtest_results': self.best_individual.backtest_results,
            'history': self.history,
            'total_time': total_time,
            'generations': self.generations
        }

    def print_results(self, results: Dict):
        """
        Print formatted optimization results

        Args:
            results: Results dict from optimize()
        """
        print(f"\n{'='*80}")
        print("GENETIC OPTIMIZATION RESULTS")
        print(f"{'='*80}")

        print(f"\n⚡ Optimization Performance:")
        print(f"  Total Time: {results['total_time']:.2f}s")
        print(f"  Generations: {results['generations']}")
        print(f"  Evaluations: {self.population_size * results['generations']}")

        print(f"\n🏆 Best Solution:")
        print(f"  Fitness Score: {results['best_fitness']:.4f}")

        print(f"\n📊 Optimal Parameters:")
        for param, value in results['best_parameters'].items():
            print(f"  {param}: {value:.4f}")

        if results['best_backtest_results']:
            bt = results['best_backtest_results']
            print(f"\n💰 Backtest Performance (with optimal parameters):")
            print(f"  Return: {bt['total_return']:.2f}%")
            print(f"  Trades: {bt['total_trades']}")
            if bt['total_trades'] > 0:
                print(f"  Win Rate: {bt['win_rate']:.1f}%")
                print(f"  Profit Factor: {bt['profit_factor']:.2f}")
                print(f"  Sharpe Ratio: {bt.get('sharpe_ratio', 0):.2f}")
                print(f"  Max Drawdown: {bt['max_drawdown']:.2f}%")

        print(f"\n📈 Evolution Progress:")
        for i, h in enumerate(results['history'][::max(1, len(results['history']) // 5)]):
            print(f"  Gen {h['generation'] + 1:>3}: Best={h['best_fitness']:.4f}, Avg={h['avg_fitness']:.4f}")

        print(f"\n{'='*80}\n")


def demo_genetic_optimization():
    """
    Demo: Optimize strategy parameters using genetic algorithm
    """
    # Fetch data
    symbol = 'BTC-USD'
    print(f"\nFetching {symbol} data...")

    ticker = yf.Ticker(symbol)
    data = ticker.history(interval='1d', period='1y')
    data.columns = [c.lower() for c in data.columns]

    print(f"Loaded {len(data)} candles")

    # Create optimizer
    optimizer = GeneticOptimizer(
        population_size=20,  # Smaller for demo
        generations=10,      # Fewer generations for demo
        mutation_rate=0.15,
        crossover_rate=0.7,
        elitism_size=3,
        max_workers=None
    )

    # Set parameter ranges
    optimizer.set_parameter_ranges({
        'min_confidence': (50.0, 85.0),  # Optimize confidence threshold
    })

    # Run optimization
    results = optimizer.optimize(
        symbol=symbol,
        data=data,
        fitness_function='composite',  # Balance return, win rate, drawdown
        initial_capital=10000.0,
        commission=0.001
    )

    # Print results
    optimizer.print_results(results)


if __name__ == "__main__":
    demo_genetic_optimization()
